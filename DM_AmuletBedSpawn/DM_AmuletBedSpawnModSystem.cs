using DM_AmuletBedSpawn.Configuration;
using HarmonyLib;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
 

namespace DM_AmuletBedSpawn
{
    public class DM_AmuletBedSpawnModSystem : ModSystem
    {
        internal static AmuletBedSpawnConfig Config { get; private set; }
        internal static readonly Harmony _harmony = new("amuletbedspawn");

        public override void StartServerSide(ICoreServerAPI api)
        {
            Config = ModConfig.ReadConfig(api);
            _harmony.PatchAll();
            api.Event.PlayerRespawn += OnPlayerRespawn;
            api.Event.PlayerDeath += OnPlayerDeath;
            base.StartServerSide(api);
        }

        /// <summary>
        /// Check if the block is a bed, and if it is, set the player's spawn point to the bed's location.
        /// </summary>
        public static void SetPlayerSpawn(IServerPlayer player, BlockSelection sel, AmuletType amuletType)
        {
            var block = player.Entity.World.BlockAccessor.GetBlock(sel.Position);

            if (block is not BlockBed) { return; }

            var normalizedPosition = BetBedHeadPosition(block, sel.Position);
            var currentSpawnPos = player.GetSpawnPosition(false).AsBlockPos;

            if (currentSpawnPos == normalizedPosition) { return; }

            player.SetSpawnPosition(new(normalizedPosition.X, normalizedPosition.Y, normalizedPosition.Z));
            player.WorldData.SetModData(ModConstants.SpawnSetByAmuletBedSpawnMod, true);
            player.WorldData.SetModData(ModConstants.BedIsMissing, false);

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
            player.SendLocalisedMessage(0, $"(Amulet Bed Spawn) Spawn point has been set with your {amuletType.ToItemName().ToLowerInvariant()}.");
        }

        /// <summary>
        /// This bed is being removed. Set all players with this spawn point to their original location.
        /// </summary>
        public void OnBlockRemoved(BlockBed block, IWorldAccessor world, BlockPos pos)
        {
            var normalizedPosition = BetBedHeadPosition(block, pos);
            var playersWithThisSpawn = world.AllPlayers.OfType<IServerPlayer>().Where(f => f.GetSpawnPosition(false).AsBlockPos == normalizedPosition).ToList();

            foreach (var player in playersWithThisSpawn)
            {
                ClearPlayersSpawnPoint(player);
                player.WorldData.SetModData(ModConstants.BedIsMissing, true);
                player.BroadcastPlayerData();
                player.SendLocalisedMessage(0, "(Amulet Bed Spawn) Your spawn point has been removed.");
            }
        }

        /// <summary>
        /// Determines whether the specified player is currently wearing a recognized amulet.
        /// </summary>
        /// <remarks>This method searches all inventories associated with the player for specific amulet
        /// items. If the player is null or has no inventories, the method returns false and amuletType is set to
        /// AmuletType.None.</remarks>
        /// <param name="player">The player whose inventories are checked for the presence of an amulet. This parameter must not be null.</param>
        /// <param name="amuletType">When this method returns, contains the <see cref="AmuletType"/> of the amulet found if the player is
        /// wearing one; otherwise, <see cref="AmuletType.None"/>.</param>
        /// <returns>true if the player is wearing a recognized amulet; otherwise, false.</returns>
        public static bool PlayerIsWearingAmulet(IServerPlayer player, out AmuletType amuletType)
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

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            if (byPlayer == null) { return; }

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
                    // for testing, lets just break the amulet and delete their spawn point.
                    BreakPlayersAmulet(byPlayer, amuletType);
                    ClearPlayersSpawnPoint(byPlayer);
                    byPlayer.SendLocalisedMessage(0, $"(Amulet Bed Spawn) Your {amuletType.ToItemName().ToLowerInvariant()} has broken! Spawn point removed.");
                }
            }
        }

        private void BreakPlayersAmulet(IServerPlayer player, AmuletType amuletType)
        {
            if (player == null) { return; }

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

            // If the spawn point was set by an amulet and they are not wearing the correct amulet, clear the spawn point and send them a message.
            if (byPlayer.WorldData.GetModData<bool>(ModConstants.SpawnSetByAmuletBedSpawnMod))
            {
                bool wearingAmulet = PlayerIsWearingAmulet(byPlayer, out AmuletType amuletType);

                if (!wearingAmulet)
                {
                    ClearPlayersSpawnPoint(byPlayer);
                    byPlayer.SendLocalisedMessage(0, "(Amulet Bed Spawn) You died without wearing your amulet! Spawn point removed.");
                }
                else
                {
                    // They are wearing an amulet. Lets check to see if it's the correct amulet. If not, clear the spawn point and send them a message.
                    if (byPlayer.WorldData.GetModData<bool>(ModConstants.TemporalAmuletUsed) && amuletType != AmuletType.TemporalGearAmulet)
                    {
                        ClearPlayersSpawnPoint(byPlayer);
                        byPlayer.SendLocalisedMessage(0, "(Amulet Bed Spawn) You died without wearing the correct amulet! Spawn point removed.");
                    }

                    if (byPlayer.WorldData.GetModData<bool>(ModConstants.RustyAmuletUsed) && amuletType != AmuletType.RustyGearAmulet)
                    {
                        ClearPlayersSpawnPoint(byPlayer);
                        byPlayer.SendLocalisedMessage(0, "(Amulet Bed Spawn) You died without wearing the correct amulet! Spawn point removed.");
                    }
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
            player.BroadcastPlayerData();
        }

        /// <summary>
        /// Returns the position of the head of the bed.
        /// </summary>
        private static BlockPos BetBedHeadPosition(Block bed, BlockPos pos)
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
