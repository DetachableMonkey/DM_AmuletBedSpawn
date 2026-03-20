using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DM_AmuletBedSpawn.Patches
{
    [HarmonyPatchCategory("Client")]
    [HarmonyPatch(typeof(BlockBed), nameof(BlockBed.GetPlacedBlockInteractionHelp))]
    public class BlockBedGetPlacedBlockInteractionHelpPatch
    {
        public static void Postfix(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref WorldInteraction[] __result)
        {
            if (world.Api.Side != EnumAppSide.Client) { return; }
            if (forPlayer == null) { return; }
            if (forPlayer is not IClientPlayer clientPlayer) { return; }

            try
            {
                if (DM_AmuletBedSpawnModSystem.PlayerIsWearingAmulet(clientPlayer, out AmuletType amuletType))
                {
                    BlockPos normalizedPosition = DM_AmuletBedSpawnModSystem.GetBedHeadPosition(selection.Block, selection.Position);
                    var spawnPosTree = clientPlayer.Entity.WatchedAttributes.GetTreeAttribute(ModConstants.AmuletBedSpawnPosition);

                    bool isCurrentSpawn = spawnPosTree != null
                        && normalizedPosition.X == spawnPosTree.GetInt("x")
                        && normalizedPosition.Y == spawnPosTree.GetInt("y")
                        && normalizedPosition.Z == spawnPosTree.GetInt("z");

                    __result = __result.Append(new WorldInteraction
                    {
                        ActionLangCode = isCurrentSpawn
                            ? "dm_amuletbedspawn:blockhelp-bed-spawnalreadyset"
                            : "dm_amuletbedspawn:blockhelp-bed-setspawn",
                        MouseButton = EnumMouseButton.Right
                    });
                }
            }
            catch (System.Exception)
            {

                
            }
        }
    }
}
