using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace AWearableLight
{
    [HarmonyPatch(typeof(EntityPlayer))]
    [HarmonyPatch("LightHsv", MethodType.Getter)]
    public class EntityPlayer_LightHsv_Patched
    {
        [HarmonyPostfix]
        static void Harmony_EntityPlayer_LightHsv_Postfix(EntityPlayer __instance, ref byte[] __result)
        {
            // Check for null instance, player not alive, or player in spectator mode
            if (__instance == null || !__instance.Alive || __instance.Player == null || __instance.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
                return;

            // Iterate through gear inventory slots
            foreach (var slots in __instance.GearInventory)
            {
                
                if (slots == null || slots.Empty) continue;

                if (slots.Itemstack.Collectible.Code.EndVariant() == "off") continue;

                AdjustLightLevelForItemSlot(slots, __instance, ref __result);
            }

            // Adjust light level for backpack slots

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                IInventory backpack = __instance.Player.InventoryManager.GetInventory(GlobalConstants.backpackInvClassName + "-" + __instance.PlayerUID);

                if (backpack != null)
                {
                    AdjustLightLevelForBackPackSlot((ItemSlotBackpack)backpack[slotIndex], ref __result);
                }
            }

        }

        static void AdjustLightLevelForBackPackSlot(ItemSlotBackpack itemSlotBackpack, ref byte[] __result)
        {
            // Check if slot is empty
            if (itemSlotBackpack?.Empty ?? true)
                return;

            byte[] backpackHsv = itemSlotBackpack.Itemstack.Collectible.LightHsv;

            // Check if clipOn is null
            if (backpackHsv == null)
                return;

            // If result is null, assign clipOn and return
            if (__result == null)
            {
                __result = backpackHsv;
                return;
            }

            float totalVal = __result[2] + backpackHsv[2];
            float number = backpackHsv[2] / totalVal;

            // Calculate weighted average of light values
            __result = new byte[]
            {
                (byte)(backpackHsv[0] * number + __result[0] * (1 - number)),
                (byte)(backpackHsv[1] * number + __result[1] * (1 - number)),
                Math.Max(backpackHsv[2], __result[2])
            };
        }

        static void AdjustLightLevelForItemSlot(ItemSlot gearSlots, EntityPlayer entityPlayer, ref byte[] __result)
        {
            if (gearSlots.Empty || gearSlots == null) 
                return;

            ItemStack itemStack = gearSlots.Itemstack;

            // Check if itemStack is null
            if (itemStack == null)
                return;
            
            byte[] gearHsv = gearSlots.Itemstack.Collectible.LightHsv;

            // Check if light is null
            if (gearHsv == null)
                return;

            // If result is null, assign light and return
            if (__result == null)
            {
                __result = gearHsv;
                return;
            }

            float totalVal = __result[2] + gearHsv[2];
            float number = gearHsv[2] / totalVal;

            // Calculate weighted average of light values
            __result = new byte[]
            {
                (byte)(gearHsv[0] * number + __result[0] * (1 - number)),
                (byte)(gearHsv[1] * number + __result[1] * (1 - number)),
                Math.Max(gearHsv[2], __result[2])
            };
        }
    }
}