using DM_AmuletBedSpawn.Configuration;
using HarmonyLib;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
 

namespace DM_AmuletBedSpawn
{
    public class DM_AmuletBedSpawnModSystem : ModSystem
    {
        internal static AmuletBedSpawnConfig Config { get; private set; } = null!;
        internal static readonly Harmony _harmony = new("amuletbedspawn");
        internal static BlockPos CurrentSpawnPosition = new BlockPos(0, 0, 0);

        public override void StartServerSide(ICoreServerAPI api)
        {
            // Read this mod's config file.
            Config = ModConfig.ReadConfig(api);

            // Apply Harmony patches to the game code.
            _harmony.PatchAll();

            api.Event.PlayerRespawn += OnPlayerRespawn;
            api.Event.PlayerDeath += OnPlayerDeath;
            
            base.StartServerSide(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }


        /// <summary>
        /// Check if the block is a bed, and if it is, set the player's spawn point to the bed's location.
        /// </summary>
        public static void SetPlayerSpawn(IServerPlayer player, BlockSelection sel, AmuletType amuletType)
        {
            var block = player.Entity.World.BlockAccessor.GetBlock(sel.Position);

            if (block is not BlockBed) { return; }
            
            // If the config option "AllowHayBeds" is false, then don't allow this player to set the spawn point
            // if they are trying to set it on a hay bed.
            string blockCode = $"{block.FirstCodePart()}-{block.FirstCodePart(1)}";
            
            if (!Config.AllowHayBeds && blockCode == "bed-hay") { return; }

            // If the config option "AllowRustyGearAmulets" is false, and the player is wearing one or isn't wearing anything at all
            // then don't allow this player to set the spawn point.
            if (!Config.AllowRustyGearAmulets && (amuletType == AmuletType.RustyGearAmulet || amuletType == AmuletType.None)) { return; }

            // Get the position of the head of the bed that the player is clicking on.
            var normalizedPosition = GetBedHeadPosition(block, sel.Position);
            var currentSpawnPos = player.GetSpawnPosition(false).AsBlockPos;

            // If the player's current spawn point is already set to this bed's location...
            if (currentSpawnPos == normalizedPosition) 
            {
                // Check to see if the WatchedAttributes for this playyer is null. If so, we need to initialize it so we can check it properly in the future.
                var spawnPosTree = player.Entity.WatchedAttributes.GetTreeAttribute(ModConstants.AmuletBedSpawnPosition);

                if (spawnPosTree == null)
                {
                    // Add this watched attribute.
                    spawnPosTree = player.Entity.WatchedAttributes.GetOrAddTreeAttribute(ModConstants.AmuletBedSpawnPosition);
                    spawnPosTree.SetInt("x", normalizedPosition.X);
                    spawnPosTree.SetInt("y", normalizedPosition.Y);
                    spawnPosTree.SetInt("z", normalizedPosition.Z);
                    player.Entity.WatchedAttributes.MarkAllDirty();
                }

                return; 
            }

            // Set the player's spawn point.
            player.SetSpawnPosition(new(normalizedPosition.X, normalizedPosition.Y, normalizedPosition.Z));
            player.WorldData.SetModData(ModConstants.SpawnSetByAmuletBedSpawnMod, true);
            player.WorldData.SetModData(ModConstants.BedIsMissing, false);
            var posTree = player.Entity.WatchedAttributes.GetOrAddTreeAttribute(ModConstants.AmuletBedSpawnPosition);
            posTree.SetInt("x", normalizedPosition.X);
            posTree.SetInt("y", normalizedPosition.Y);
            posTree.SetInt("z", normalizedPosition.Z);
            player.Entity.WatchedAttributes.MarkAllDirty();

            if (amuletType == AmuletType.TemporalGearAmulet)
            {
                player.WorldData.SetModData(ModConstants.TemporalAmuletUsed, true);
                player.WorldData.SetModData(ModConstants.RustyAmuletUsed, false);
            }
            else if (amuletType == AmuletType.RustyGearAmulet)
            {
                player.WorldData.SetModData(ModConstants.RustyAmuletUsed, true);
                player.WorldData.SetModData(ModConstants.TemporalAmuletUsed, false);
            }

            player.BroadcastPlayerData();
            player.SendLocalisedMessage(0, $"[Amulet Bed Spawn] Spawn point has been set with your {amuletType.ToItemName().ToLowerInvariant()}.");
        }

        /// <summary>
        /// This bed is being removed. Set all players with this spawn point to their original location.
        /// </summary>
        public void OnBlockRemoved(BlockBed block, IWorldAccessor world, BlockPos pos)
        {
            if (!Config.BreakingBedRemovesSpawnPoint) { return; }

            var normalizedPosition = GetBedHeadPosition(block, pos);
            var playersWithThisSpawn = world.AllPlayers.OfType<IServerPlayer>().Where(f => f.GetSpawnPosition(false).AsBlockPos == normalizedPosition).ToList();

            foreach (var player in playersWithThisSpawn)
            {
                ClearPlayersSpawnPoint(player);
                player.WorldData.SetModData(ModConstants.BedIsMissing, true);
                player.BroadcastPlayerData();
                player.SendLocalisedMessage(0, "[Amulet Bed Spawn] Your spawn point has been removed.");
            }
        }

        /// <summary>
        /// Determines whether the specified player is currently wearing a recognized amulet.
        /// </summary>
        /// <param name="player">The player whose inventories are checked for the presence of an amulet. This parameter must not be null.</param>
        /// <param name="amuletType">When this method returns, contains the <see cref="AmuletType"/> of the amulet found if the player is
        /// wearing one; otherwise, <see cref="AmuletType.None"/>.</param>
        /// <returns>true if the player is wearing a recognized amulet; otherwise, false.</returns>
        public static bool PlayerIsWearingAmulet(IPlayer player, out AmuletType amuletType)
        {
            amuletType = AmuletType.None;

            if (player == null) return false;

            foreach (var inventory in player.InventoryManager.Inventories)
            {
                IInventory inv = inventory.Value;

                if (inv.ClassName == "character")
                {
                    foreach (ItemSlot slot in inv)
                    {
                        string stackName = slot.GetStackName();

                        if (stackName == AmuletType.TemporalGearAmulet.ToItemName())
                        {
                            amuletType = AmuletType.TemporalGearAmulet;
                            return true;
                        }
                        else if (stackName == AmuletType.RustyGearAmulet.ToItemName())
                        {
                            amuletType = AmuletType.RustyGearAmulet;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// This event fires immediately after the player respawns, but before they have control of their character again.
        /// </summary>
        /// <param name="byPlayer"></param>
        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            if (byPlayer == null) { return; }
            if (!Config.AmuletsCanBreakAfterRespawn) { return; }

            // Verify that the player set their spawn point with this mod. If not, just proceed with normal respawn behavior.
            if (byPlayer.WorldData.GetModData<bool>(ModConstants.SpawnSetByAmuletBedSpawnMod))
            {
                // Check to see if the player is currently wearing the type of amulet that they used to set their spawn point.
                bool wearingCorrectAmulet = false;
                AmuletType amuletType = AmuletType.None;

                if (PlayerIsWearingAmulet(byPlayer, out amuletType))
                {
                    if (byPlayer.WorldData.GetModData<bool>(ModConstants.TemporalAmuletUsed) && amuletType == AmuletType.TemporalGearAmulet)
                    {
                        wearingCorrectAmulet = true;
                    }

                    if (byPlayer.WorldData.GetModData<bool>(ModConstants.RustyAmuletUsed) && amuletType == AmuletType.RustyGearAmulet)
                    {
                        wearingCorrectAmulet = true;
                    }
                }

                // If the player is wearing the correct amulet, lets see if their amulet is going to break during this respawn.
                if (wearingCorrectAmulet)
                {
                    // Get a random number between 0 and 1.
                    double random = byPlayer.Entity.World.Rand.NextDouble();

                    // Convert Config.RustyGearAmuletBreakChancePct to a percentage.
                    double breakChance = 0;

                    if (amuletType == AmuletType.RustyGearAmulet)
                    {
                        breakChance = Config.RustyGearAmuletBreakChancePct / 100d;
                    }
                    else if (amuletType == AmuletType.TemporalGearAmulet)
                    {
                        breakChance = Config.TemporalGearAmuletBreakChancePct / 100d;
                    }

                    // If the random number is less than the break chance, break the amulet and clear their spawn point.
                    if (random < breakChance)
                    {
                        BreakPlayersAmulet(byPlayer, amuletType);
                        ClearPlayersSpawnPoint(byPlayer);
                        byPlayer.SendLocalisedMessage(0, $"[Amulet Bed Spawn] Your {amuletType.ToItemName().ToLowerInvariant()} has broken! Spawn point removed.");
                    }
                }
            }
        }

        /// <summary>
        /// This will remove/delete the amulet from the player's neck slot.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amuletType"></param>
        private void BreakPlayersAmulet(IServerPlayer player, AmuletType amuletType)
        {
            if (player == null) { return; }
            if (!Config.AmuletsCanBreakAfterRespawn) { return; }

            string amuletItemName = amuletType.ToItemName();

            foreach (var inventory in player.InventoryManager.Inventories)
            {
                IInventory inv = inventory.Value;

                if (inv.ClassName == "character")
                {
                    foreach (ItemSlot slot in inv)
                    {
                        if (slot.GetStackName() == amuletItemName)
                        {
                            slot.TakeOut(slot.StackSize);
                            slot.MarkDirty();
                        }
                    }
                }
            }
        }

        private void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            if (byPlayer == null) { return; }
            if (!Config.AmuletsCanBreakAfterRespawn) { return; }

            // If the spawn point was set by an amulet and they are not wearing the correct amulet, clear the spawn point and send them a message.
            if (byPlayer.WorldData.GetModData<bool>(ModConstants.SpawnSetByAmuletBedSpawnMod))
            {
                bool wearingAmulet = PlayerIsWearingAmulet(byPlayer, out AmuletType amuletType);

                if (!wearingAmulet)
                {
                    // This player isn't wearing any amulet at all. Penalize them by clearing their spawn point and send them a message.
                    ClearPlayersSpawnPoint(byPlayer);
                    byPlayer.SendLocalisedMessage(0, "[Amulet Bed Spawn] You died without wearing your amulet! Spawn point removed.");
                }
                else
                {
                    // They are wearing an amulet. If they set their spawn with a temporal gear amulet, make sure they are wearing one with the exact type.
                    // If they aren't , then clear their spawn point and send them a message.
                    if (byPlayer.WorldData.GetModData<bool>(ModConstants.TemporalAmuletUsed) && amuletType != AmuletType.TemporalGearAmulet)
                    {
                        ClearPlayersSpawnPoint(byPlayer);
                        byPlayer.SendLocalisedMessage(0, "[Amulet Bed Spawn] You died without wearing your temporal gear amulet!");
                        byPlayer.SendLocalisedMessage(0, "[Amulet Bed Spawn] Spawn point removed.");
                        return;
                    }

                    // We are assuming here that a temporal amulet is much harder to get, so lets not penalize the player for basically upgrading their amulet.
                    // If we are here in the code, then the player used a rusty amulet to set their spawn point and they've
                    // upgraded to a temporal amulet and are wearing that currently.
                    return;
                }
            }
        }

        private static void ClearPlayersSpawnPoint(IServerPlayer player)
        {
            player.ClearSpawnPosition();
            player.WorldData.SetModData(ModConstants.SpawnSetByAmuletBedSpawnMod, false);
            player.WorldData.SetModData(ModConstants.BedIsMissing, false);
            player.WorldData.SetModData(ModConstants.TemporalAmuletUsed, false);
            player.WorldData.SetModData(ModConstants.RustyAmuletUsed, false);
            player.Entity.WatchedAttributes.RemoveAttribute(ModConstants.AmuletBedSpawnPosition);
            player.Entity.WatchedAttributes.MarkAllDirty();
            player.BroadcastPlayerData();
        }

        /// <summary>
        /// Returns the position of the head of the bed.
        /// </summary>
        public static BlockPos GetBedHeadPosition(Block bed, BlockPos pos)
        {
            if (bed.Variant["part"] == "head") { return pos; }

            var currentSide = bed.Variant["side"];
            var headFacing = BlockFacing.FromCode(currentSide).Opposite;

            return pos.AddCopy(headFacing);
        }

        public override void Dispose()
        {
            base.Dispose();
            // Unpatch all patches applied by this Harmony instance when the mod system is disposed
            _harmony?.UnpatchAll(_harmony?.Id);
        }
    }
}
