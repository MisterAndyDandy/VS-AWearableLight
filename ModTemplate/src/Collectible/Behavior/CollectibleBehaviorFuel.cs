
using AWearableLight.Interfaces;
using AWearableLight.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AWearableLight
{
    public class CollectibleBehaviorFuel : CollectibleBehavior, IPowerSource
    {

        #nullable enable
        private ICoreAPI? Api { get; set; }

        public float FuelCapacity = 24f;

        public CollectibleBehaviorFuel(CollectibleObject collObj) : base(collObj) { }

        public override void OnLoaded(ICoreAPI api)
        {
            Api = api;

            FuelProps lightProps = WearableLightConfigHelper.GetProps(collObj.Code);

            FuelCapacity = lightProps.Capacity > 0 ? lightProps.Capacity : FuelCapacity;

            base.OnLoaded(api);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling)
        {
            if (outputSlot?.Itemstack != null)
            {
                SetPower(outputSlot.Itemstack, 8);
            }

            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
        }

        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority, ref EnumHandling handling)
        {
            if (priority == EnumMergePriority.DirectMerge)
            {
                if (!WearableLightConfigHelper.NeedFuel(sinkStack.Collectible.Code))
                {
                    return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
                }

                float fuel = GetStackFuel(sourceStack, sinkStack);

                if (fuel <= 0f)
                {
                    return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
                }

                double fuelHours = GetPower(sinkStack);

                handling = EnumHandling.PreventDefault;

                if (fuelHours >= FuelCapacity)
                {
                    return 0;
                }

                return 1;
            }

            return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
        }

        public override void TryMergeStacks(ItemStackMergeOperation op, ref EnumHandling handling)
        {
            if (op.CurrentPriority != EnumMergePriority.DirectMerge)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            if (op.SourceSlot.Empty || op.SinkSlot.Empty)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            ItemStack sourceStack = op.SourceSlot.Itemstack;
            ItemStack sinkStack = op.SinkSlot.Itemstack;

            if (sourceStack == null || sinkStack == null)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            if (!WearableLightConfigHelper.NeedFuel(sinkStack.Collectible.Code))
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            float stackFuel = GetStackFuel(sourceStack, sinkStack);

            if (stackFuel <= 0f)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            double fuelHours = GetPower(sinkStack);

            if (fuelHours >= FuelCapacity)
            {
                if (Api?.Side == EnumAppSide.Client)
                {
                    (Api as ICoreClientAPI)?.TriggerIngameError(
                        this,
                        "lightfull",
                        Lang.Get(
                            "awearablelight:ingameerror-light-full",
                            sinkStack.Collectible.GetHeldItemName(sinkStack)
                        )
                    );
                }

                handling = EnumHandling.PreventDefault;
                return;
            }

            AddPower(sourceStack, sinkStack, stackFuel);

            op.MovedQuantity = 1;
            op.SourceSlot.TakeOut(1);
            op.SinkSlot.MarkDirty();

            handling = EnumHandling.PreventDefault;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (inSlot == null || inSlot.Empty) return;

            ItemStack itemStack = inSlot.Itemstack;

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (!WearableLightConfigHelper.NeedFuel(itemStack.Collectible.Code))
            {
                return;
            }

            var fuel = GetPower(itemStack).ToString("0.0");
            var capfuel = FuelCapacity.ToString("0.0");

            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("awearablelight:fuel-hours", fuel, capfuel));

        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            if (inSlot.Empty) return base.GetHeldInteractionHelp(inSlot, ref handling);

            var props = WearableLightConfigHelper.GetProps(inSlot.Itemstack.Collectible.Code);

            if (!props.UseFuel || props.FuelList == null || props.FuelList.Count == 0)
            {
                return base.GetHeldInteractionHelp(inSlot, ref handling);
            }

            List<ItemStack> fuelStacks = new();

            foreach (var fuel in props.FuelList.Keys)
            {
                if (string.IsNullOrEmpty(fuel)) continue;

                var asset = new AssetLocation(fuel);

                // Exact match item
                var item = Api?.World.GetItem(asset);

                if (item != null)
                {
                    fuelStacks.Add(new ItemStack(item));
                    continue;
                }

                // Wildcard match
                var matches = Api?.World.SearchItems(asset);

                if (matches != null)
                {
                    foreach (var match in matches)
                    {
                        fuelStacks.Add(new ItemStack(match));
                    }
                }
            }

            if (fuelStacks.Count == 0)
            {
                return base.GetHeldInteractionHelp(inSlot, ref handling);
            }

            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "awearablelight:heldhelp-fueltypes",
                    MouseButton = EnumMouseButton.None,
                    Itemstacks = fuelStacks.ToArray()
                }
            }
            .Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }

        public virtual float GetStackFuel(ItemStack sourceStack, ItemStack sinkStack)
        {
            if (sourceStack == null || sinkStack == null) return 0f;

            var props = WearableLightConfigHelper.GetProps(sinkStack.Collectible.Code);

            if (!props.UseFuel) return 0f;

            if (props.FuelList == null || props.FuelList.Count == 0) return 0f;

            AssetLocation sourceCode = sourceStack.Collectible.Code;

            foreach (var (code, value) in props.FuelList)
            {
                if (WildcardUtil.Match(new(code), sourceCode))
                {
                    return value;
                }
            }

            return 0f;
        }

        public virtual double GetPower(ItemStack sourceStack)
        {
            return sourceStack.Attributes.GetDouble("fuelHours", 0);
        }

        public virtual void SetPower(ItemStack sourceStack, double amount)
        {
            sourceStack.Attributes.SetDouble("fuelHours", GameMath.Clamp(amount, 0, FuelCapacity));
        }

        public virtual void ConsumePower(ItemSlot slot, EntityPlayer entityPlayer, double amount)
        {
            if (slot?.Itemstack?.Attributes == null) return;

            var attachmentableLight =
                slot.Itemstack.Collectible
                    .GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

            if (attachmentableLight == null) return;

            // Only consume while active
            if (!attachmentableLight.LightState(slot.Itemstack))
            {
                return;
            }

            double fuel = GetPower(slot.Itemstack);

            SetPower(slot.Itemstack, fuel - amount);

            fuel = GetPower(slot.Itemstack);

            if (fuel <= 0)
            {
                attachmentableLight.SetLightActive(
                    slot,
                    false,
                    entityPlayer
                );
            }

            slot.MarkDirty();
        }

        public virtual void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount)
        {
            if (sourceStack == null) return;

            SetPower(sinkStack, GetPower(sinkStack) + amount);
        }

        public virtual bool HasPower(ItemStack sourceStack)
        {
            return GetPower(sourceStack) > 0;
        }

        public virtual bool IsFull(ItemStack sourceStack)
        {
            return GetPower(sourceStack) >= FuelCapacity;
        }
    }
}