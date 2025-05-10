using System.Linq;
using System.Numerics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using AWearableLight.Config;
using Vintagestory.API.Server;
using ProtoBuf;
using System.Net.Sockets;


namespace AWearableLight
{
    class ItemAttachmentableLight : ItemWearable
    {
        
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return null;
        }

        public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
        {
            return null;
        }

        public void OnUsed(ItemSlot itemSlot, EntityPlayer entityPlayer)
        {
            if (AWearableLightCore.Config.EnableSound)
            {
                JsonObject attSound = Attributes["sound"];
                if (attSound.Exists)
                {
                    if (attSound.AsString() != null)
                    {
                        AssetLocation soundLocation = new AssetLocation(attSound.ToString());

                        if (soundLocation == null) return;

                        entityPlayer.World.PlaySoundAt(soundLocation, entityPlayer.Pos.X + 0.5, entityPlayer.Pos.Y + 0.75, entityPlayer.Pos.Z + 0.5, null, randomizePitch: false, volume: 16f);
                    };
                }
                else
                {
                    api.Logger.Error($"Cannot find sound attribute for item: {Code.GetName().ToLower()}");
                }
            }

            ItemStack itemStack = itemSlot.Itemstack;

            if (itemStack == null) return;

            itemSlot.Itemstack = new ItemStack(api.World.GetItem(new AssetLocation(Code.Domain, GetAssetLocation)))
            {
                Attributes = itemSlot.Itemstack.Attributes.Clone()
            };

            entityPlayer.Player.InventoryManager.BroadcastHotbarSlot();

            itemSlot.MarkDirty();
        }

        public string GetAssetLocation => Code.GetName().Substring(0, Code.GetName().Length - Code.EndVariant().Length) + (Code.EndVariant() == "off" ? "on" : "off");

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            #region Light
            // Ensure itemStack and Collectible are not null
            ItemStack itemStack = inSlot.Itemstack;

            byte[] lightHsv = itemStack.Collectible.LightHsv;

            // If lightHsv is not available, use original values
            if (lightHsv[0] == 0)
            {
                lightHsv = new ItemStack(world.GetItem(new AssetLocation(Code.Domain, GetAssetLocation))).Collectible.LightHsv;
            }

            // Constants for indices
            const int ValueIndex = 2;

            // Check light level and append to description
            dsc.Append(lightHsv[ValueIndex] > 0 ? $"{Lang.Get("light-level")}{lightHsv[ValueIndex]}\n" : "");

            // Continue with the base method
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            #endregion
        }
 
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1]
            {
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("heldhelp-toggle"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "toggleLight"
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
