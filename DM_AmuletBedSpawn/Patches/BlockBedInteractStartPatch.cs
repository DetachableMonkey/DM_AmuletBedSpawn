using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DM_AmuletBedSpawn.Patches
{
    [HarmonyPatchCategory("Server")]
    [HarmonyPatch(typeof(BlockBed), nameof(BlockBed.OnBlockInteractStart))]
    public class BlockBedInteractStartPatch
    {
        public static void Postfix(BlockBed __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Api.Side != EnumAppSide.Server) { return; }
            if (byPlayer is not IServerPlayer serverPlayer) { return; }

            // Check if the player has the required amulet in their inventory, and is wearing it.
            if (DM_AmuletBedSpawnModSystem.PlayerIsWearingAmulet(serverPlayer, out AmuletType amuletType))
            {
                // The player has the required amulet, so we can set their spawn point to the bed they just interacted with.
                DM_AmuletBedSpawnModSystem.SetPlayerSpawn(serverPlayer, blockSel, amuletType);
            }
        }
    }
}
