using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AWearableLight.Util
{
    [HarmonyPatch(typeof(EntityPlayer))]
    [HarmonyPatch("LightHsv", MethodType.Getter)]
    public class EntityPlayer_LightHsv_Patched
    {

        [HarmonyPostfix]
        static void Harmony_EntityPlayer_LightHsv_Postfix(EntityPlayer __instance, ref byte[] __result)
        {
            // Check for null instance, player not alive, or player in spectator mode

            try
            {
                if (__instance == null || !__instance.Alive || __instance.Player == null || __instance.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
                    return;

                PlayerLightHsvData.LightHsvData(__instance, ref __result);
            }
            catch 
            {

            }
        }
    }

    public class PlayerLightHsvData
    {

        public static void LightHsvData(EntityPlayer entityPlayer, ref byte[] lightHsv)
        {
            // Your custom logic here to process the light HSV data or perform other actions
            // For example, adjust the light HSV values based on player conditions or settings
            // This method will be called after the getter has been invoked
            // Iterate through gear inventory slots

            foreach (var slots in entityPlayer.Player.InventoryManager.GetInventory(GlobalConstants.characterInvClassName + "-" +entityPlayer.PlayerUID))
            {

                if (slots == null || slots.Empty) continue;

                if (slots.Itemstack.Collectible?.LightHsv[0] == 0) continue;

                AdjustLightLevelForItemSlot(slots, entityPlayer, ref lightHsv);
            }

            // Adjust light level for backpack slots

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                IInventory backpack = entityPlayer.Player.InventoryManager.GetInventory(GlobalConstants.backpackInvClassName + "-" + entityPlayer.PlayerUID);

                if (backpack != null)
                {
                    AdjustLightLevelForBackPackSlot((ItemSlotBackpack)backpack[slotIndex], ref lightHsv);
                }
            }

            static void AdjustLightLevelForBackPackSlot(ItemSlotBackpack itemSlotBackpack, ref byte[] lightHsv)
            {
                // Check if slot is empty
                if (itemSlotBackpack?.Empty ?? true)
                    return;

                byte[] backpackHsv = itemSlotBackpack.Itemstack.Collectible.LightHsv;

                // Check if clipOn is null
                if (backpackHsv == null)
                    return;

                // If result is null, assign clipOn and return
                if (lightHsv == null)
                {
                    lightHsv = backpackHsv;
                    return;
                }

                float totalVal = lightHsv[2] + backpackHsv[2];
                float number = backpackHsv[2] / totalVal;

                // Calculate weighted average of light values
                lightHsv = new byte[]
                {
                (byte)(backpackHsv[0] * number + lightHsv[0] * (1 - number)),
                (byte)(backpackHsv[1] * number + lightHsv[1] * (1 - number)),
                Math.Max(backpackHsv[2], lightHsv[2])
                };
            }

            static void AdjustLightLevelForItemSlot(ItemSlot gearSlots, EntityPlayer entityPlayer, ref byte[] lightHsv)
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
                if (lightHsv == null)
                {
                    lightHsv = gearHsv;
                    return;
                }

                float totalVal = lightHsv[2] + gearHsv[2];
                float number = gearHsv[2] / totalVal;

                // Calculate weighted average of light values
                lightHsv = new byte[]
                {
                (byte)(gearHsv[0] * number + lightHsv[0] * (1 - number)),
                (byte)(gearHsv[1] * number + lightHsv[1] * (1 - number)),
                Math.Max(gearHsv[2], lightHsv[2])
                };

                
            }
        }
    }
}