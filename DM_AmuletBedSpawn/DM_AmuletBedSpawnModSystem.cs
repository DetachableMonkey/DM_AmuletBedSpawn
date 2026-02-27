using HarmonyLib;
using System.Linq;
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
        internal static readonly Harmony _harmony = new("dm_amuletbedspawn");

        public override void StartServerSide(ICoreServerAPI api)
        {
            _harmony.PatchCategory("Server");
            base.StartServerSide(api);
        }

        /// <summary>
        /// Check if the block is a bed, and if it is, set the player's spawn point to the bed's location.
        /// </summary>
        public void SetPlayerSpawn(IServerPlayer player, BlockSelection sel)
        {
            var block = player.Entity.World.BlockAccessor.GetBlock(sel.Position);

            if (block is not BlockBed) { return; }

            var normalizedPosition = BetBedHeadPosition(block, sel.Position);
            var currentSpawnPos = player.GetSpawnPosition(false).AsBlockPos;

            if (currentSpawnPos == normalizedPosition) { return; }

            player.SetSpawnPosition(new(normalizedPosition.X, normalizedPosition.Y, normalizedPosition.Z));
            player.WorldData.SetModData(ModConstants.BedIsMissing, false);
            player.BroadcastPlayerData();
            player.SendLocalisedMessage(0, "(Amulet Bed Spawn) Spawn point has been set.");
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
                player.ClearSpawnPosition();
                player.WorldData.SetModData(ModConstants.BedIsMissing, true);
                player.BroadcastPlayerData();
                player.SendLocalisedMessage(0, "(Amulet Bed Spawn) Your spawn point has been removed.");
            }
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
