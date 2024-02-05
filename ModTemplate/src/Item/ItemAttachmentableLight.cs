using AWearableLight.Util;
using AWearableLight;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AWearableLight
{
    class ItemAttachmentableLight : ItemWearable
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
         
            if (slot == null || byEntity == null) return;

            if (handHandling != EnumHandHandling.PreventDefaultAnimation) {
                
                if (byEntity.Controls.CtrlKey)
                {
                    byEntity.Attributes.SetInt("use", 1);
                    byEntity.Attributes.SetInt("useCancel", 0);
                    ///byEntity.AnimManager.StartAnimation("interactstatic");
                    handHandling = EnumHandHandling.PreventDefaultAnimation;
                    return;
                }
            }
       
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return null;
        }

        public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
        {
            return null;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            return secondsUsed < 1f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(byEntity.Attributes.GetInt("usedCancel") == 1)

            if (secondsUsed < 1f)
            {
                    return;
            }

            if (!Config.Current.DisableSound.Value)
            {
                if (Attributes["sound"]?.Exists == true)
                {
                    var soundLocation = new AssetLocation(Attributes["sound"].ToString());
                    byEntity.World.PlaySoundAt(soundLocation, byEntity.Pos.X + 0.5, byEntity.Pos.Y + 0.75, byEntity.Pos.Z + 0.5, null, randomizePitch: false, volume: 16f);
                }
                else
                {
                    api.Logger.Error($"Cannot find sound attribute for item: {Code.GetName().ToLower()}");
                }
            }

            byEntity.Attributes.SetInt("using", 1);

            //byEntity.AnimManager.StopAnimation("interactstatic");

            slot.Itemstack = new ItemStack(api.World.GetItem(new AssetLocation(Code.Domain, GetAssetLocation())))
            {
                Attributes = slot.Itemstack.Attributes.Clone()
            };

            slot.MarkDirty();

            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.Attributes.SetInt("using", 0);
            //byEntity.AnimManager.StopAnimation("interactstatic");
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            if (cancelReason == EnumItemUseCancelReason.ReleasedMouse)
            {
                byEntity.Attributes.SetInt("usedCancel", 1);
                return true;
            }

            return false;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            #region Light
            // Ensure itemStack and Collectible are not null
            ItemStack itemStack = inSlot.Itemstack;
            if (itemStack == null || itemStack.Collectible == null || itemStack.Collectible.LightHsv == null)
            {
                // Handle null case or return early
                return;
            }

            byte[] lightHsv = GetLightHsv(world.BlockAccessor, null, itemStack);

            // If lightHsv is not available, use original values
            if (lightHsv.First() == 0)
            {
                lightHsv = new ItemStack(world.GetItem(new AssetLocation(Code.Domain, GetAssetLocation()))).Collectible.LightHsv;
            }

            // Constants for indices
            const int ValueIndex = 2;

            // Check light level and append to description
            dsc.Append(lightHsv[ValueIndex] > 0 ? $"{Lang.Get("light-level")}{lightHsv[ValueIndex]}\n" : "");

            
            // Continue with the base method
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            #endregion
        }

        public virtual string GetAssetLocation() => Code.GetName().Remove(Code.GetName().Length - Code.EndVariant().Length) + (Code.EndVariant() == "off" ? "on" : "off");

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1]
            {
                new WorldInteraction
                {
                    ActionLangCode = "heldhelp-headlantern-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "ctrl"
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
