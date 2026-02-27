using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DM_AmuletBedSpawn.Patches
{
    [HarmonyPatchCategory("Server")]
    [HarmonyPatch(typeof(BlockBed), nameof(BlockBed.OnBlockRemoved))]
    public class BlockBedBlockRemovedPathc
    {
        public static void Postfix(BlockBed __instance, IWorldAccessor world, BlockPos pos)
        {
            if (world.Api.Side != EnumAppSide.Server) { return; }

            /// Call the OnBlockRemoved method to check if any players had their spawn point set to this bed, 
            /// and if so, clear their spawn point and set their "bed missing" flag to true.
            var modSystem = world.Api.ModLoader.GetModSystem<DM_AmuletBedSpawnModSystem>();
            modSystem.OnBlockRemoved(__instance, world, pos);
        }
    }
}
