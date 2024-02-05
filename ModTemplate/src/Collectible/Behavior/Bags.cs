using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace AWearableLight
{
    class CollectibleBagsBehavior : CollectibleBehavior
    {
  
        private WorldInteraction[] interactions;

        public CollectibleBagsBehavior(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client)
            {
                return;
            }

            interactions = ObjectCacheUtil.GetOrCreate(api, "bagInteractions", delegate
            {
                List<ItemStack> list = new List<ItemStack>();
                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    if (collectible.Attributes != null)
                    {
                        if (collectible.Attributes["backpack"].Exists) { list.Add(new ItemStack(collectible)); }

                    };
                }

                return new WorldInteraction[1]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "heldhelp-bags-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list.ToArray()
                    }
                };
            });
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (slot == null || byEntity == null || !(byEntity.Api is ICoreAPI api))
                return;

            if (byEntity.Controls.ShiftKey)
                return;

            if (byEntity is EntityPlayer playerEntity && playerEntity.Player is IPlayer player)
            {
                IInventory playerInventory = player.InventoryManager.GetInventory(GlobalConstants.backpackInvClassName + "-" + player.PlayerUID);

                int maxSlots = 4;

                for (int slotIndex = 0; playerInventory != null && slotIndex < maxSlots; slotIndex++)
                {
                    slot.TryPutInto(api.World, playerInventory[slotIndex]);
                    slot.MarkDirty();
                    handHandling = EnumHandHandling.PreventDefault;
                }
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }
    }
}
