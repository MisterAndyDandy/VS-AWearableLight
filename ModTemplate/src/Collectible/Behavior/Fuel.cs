using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

public interface IFuelType
{
    float FuelCapacity { get; }

    bool HasFuel(ItemStack sourceStack);

    bool IsFull(ItemStack sourceStack);

    void AddFuel(ItemStack sourceStack, ItemStack sinkStack, double amount);

    void ConsumeFuel(ItemStack sorceStack, double amount);

    void SetFuel(ItemStack sourceStack, double amount);

    double GetFuel(ItemStack sourceStack);

    bool UseFuel(ItemStack sourceStack);
}

public class CollectibleBehaviorFuel : CollectibleBehavior, IFuelType
{

    #nullable enable
    private ICoreAPI? Api {  get; set; }

    public float FuelCapacity => collObj.Attributes?["fuelCapacity"].AsFloat(24f) ?? 24f;

    public CollectibleBehaviorFuel(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        Api = api;

        base.OnLoaded(api);
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling )
    {
        if(outputSlot?.Itemstack != null)
        {
            SetFuel(outputSlot.Itemstack, 8);
        }

        base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
    }

    public override int GetMergableQuantity( ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority, ref EnumHandling handling)
    {
        if (priority == EnumMergePriority.DirectMerge)
        {
            if (!UseFuel(sinkStack))
            {
                return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
            }

            float fuel = GetStackFuel(sourceStack, sinkStack);

            if (fuel <= 0f)
            {
                return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
            }

            double fuelHours = GetFuel(sinkStack);

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

        if (!UseFuel(sinkStack))
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

        double fuelHours = GetFuel(sinkStack);

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

        AddFuel(sourceStack, sinkStack, stackFuel);

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

        if (!UseFuel(itemStack)) 
        {
            return;
        }

        var fuel = GetFuel(itemStack).ToString("0.0");
        var capfuel = FuelCapacity.ToString("0.0");

        dsc.AppendLine();
        dsc.AppendLine(Lang.Get("awearablelight:fuel-hours", fuel, capfuel));

    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        if (!inSlot.Empty && !UseFuel(inSlot.Itemstack)) return base.GetHeldInteractionHelp(inSlot, ref handling);

        List<ItemStack> fuelStacks = new List<ItemStack>();

        // Example fuel lookup from attributes
        string fuelTypes = collObj.Attributes?["fuelType"].AsString() ?? string.Empty;

        CollectibleObject? fuelItem = Api?.World.GetItem(new AssetLocation(fuelTypes));

        if (fuelItem != null)
        {
            fuelStacks.Add(new ItemStack(fuelItem));
        }

        return new WorldInteraction[]
        {
            new WorldInteraction
            {
                ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-fueltypes"),
                MouseButton = EnumMouseButton.None,
                Itemstacks = fuelStacks.ToArray()
            }
        }
        .Append(base.GetHeldInteractionHelp(inSlot, ref handling));
    }

    public float GetStackFuel(ItemStack sourceStack, ItemStack sinkStack)
    {
        bool matched = WildcardUtil.Match(new AssetLocation(sourceStack.Collectible.Attributes?["fuelType"]?.AsString() ?? string.Empty), sinkStack.Collectible.Code);

        if (matched) return 0f;

        return sourceStack.Collectible.Attributes?["stackFuelHours"]?.AsFloat() ?? 0f;
    }

    public virtual double GetFuel(ItemStack sourceStack)
    {
        return sourceStack.Attributes.GetDouble("fuelHours", 0);
    }

    public virtual void SetFuel(ItemStack sourceStack, double amount)
    {
        sourceStack.Attributes.SetDouble("fuelHours", GameMath.Clamp(amount, 0, FuelCapacity));
    }

    public virtual void ConsumeFuel(ItemStack sourceStack, double amount)
    {
        if (amount <= 0) return;

        double fuel = GetFuel(sourceStack) - amount;
        SetFuel(sourceStack, fuel);
    }

    public virtual void AddFuel(ItemStack sourceStack, ItemStack sinkStack, double amount)
    {
        if (sourceStack == null) return;

        SetFuel(sinkStack, GetFuel(sinkStack) + amount);
    }

    public virtual bool HasFuel(ItemStack sourceStack)
    {
        return GetFuel(sourceStack) > 0;
    }

    public virtual bool UseFuel(ItemStack sourceStack) 
    {
        return sourceStack.Collectible.Attributes?["useFuel"]?.AsBool(true) ?? true;
    }

    public virtual bool IsFull(ItemStack sourceStack)
    {
        return GetFuel(sourceStack) >= FuelCapacity;
    }
}