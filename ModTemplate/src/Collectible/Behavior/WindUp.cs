
using AWearableLight.Interfaces;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace AWearableLight
{
    public class CollectibleBehaviorWindup : CollectibleBehavior, IPowerSource
    {
        public CollectibleBehaviorWindup(CollectibleObject collObj) : base(collObj)
        {
        }

        public float MaxCharge => collObj.Attributes?["maxCharge"].AsFloat(300f) ?? 300f;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            handling = EnumHandling.Handled;
            handHandling = EnumHandHandling.Handled;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if (!IsFull(slot.Itemstack))
            {
                AddPower(slot.Itemstack, null, secondsUsed * 10); // tuning value
                slot.MarkDirty();
            }

            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            var fuel = GetPower(slot.Itemstack);
            _ = fuel;
            handling = EnumHandling.Handled;
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (inSlot == null || inSlot.Empty) return;

            ItemStack itemStack = inSlot.Itemstack;

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var power = GetPower(itemStack).ToString("0.0");
            var capfuel = MaxCharge.ToString("0.0");

            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("awearablelight:fuel-hours", power, capfuel));
        }

        public double GetPower(ItemStack stack)
        { 
            return stack.Attributes.GetDouble("windCharge", 0);
        }

        public void SetPower(ItemStack stack, double amount)
        { 
            stack.Attributes.SetDouble("windCharge", GameMath.Clamp(amount, 0, MaxCharge)); 
        }

        public void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount)
        {
            SetPower(sourceStack, GetPower(sourceStack) + amount);
        }

        public void ConsumePower(ItemStack stack, double amount)
        { 
            SetPower(stack, GetPower(stack) - amount); 
        }

        public bool HasPower(ItemStack stack)
        { 
            return GetPower(stack) > 0; 
        }

        public bool IsFull(ItemStack stack)
        {
            return GetPower(stack) >= MaxCharge; 
        }

        public bool NeedPower(ItemStack sourceStack)
        {
            return true;
        }

  
    }
}
