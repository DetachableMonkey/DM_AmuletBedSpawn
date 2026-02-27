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

            bool attemptToSetSpawn = false;
            string stackName;

            // Check if the player has the required amulet in their inventory, and is wearing it.
            // Acceptable Amulets: Temporal gear amulet, Rusty gear amulet
            foreach (var inventory in byPlayer.InventoryManager.Inventories)
            {
                IInventory inv = inventory.Value;

                if (inv.ClassName == "character")
                {
                    foreach (ItemSlot slot in inv)
                    {
                        stackName = slot.GetStackName();

                        // I might add in a config option later to allow users to specify their own amulets, but for now, this is fine.
                        if (stackName == "Temporal gear amulet" || stackName == "Rusty gear amulet")
                        {
                            attemptToSetSpawn = true;
                            break;
                        }
                    }
                }

                if (attemptToSetSpawn) { break; }
            }

            if (attemptToSetSpawn)
            {
                // The player has the required amulet, so we can set their spawn point to the bed they just interacted with.
                var modSystem = world.Api.ModLoader.GetModSystem<DM_AmuletBedSpawnModSystem>();
                modSystem.SetPlayerSpawn(serverPlayer, blockSel);
            }
        }
    }
}
