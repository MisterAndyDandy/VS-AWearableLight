using AWearableLight.Interfaces;
using AWearableLight.Util;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AWearableLight
{

    #nullable enable

    class CollectibleBehaviorAttachmentableLight : CollectibleBehavior
    {
        
        private ICoreAPI? api { get; set; }

        public WeatherSystemBase? WeatherSystemBase { get; set; }

        public AttachmentableLightProp? attachmentableLightProp => AttachmentableLightProp.ReadFrom(this.collObj);

        public CollectibleBehaviorAttachmentableLight(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;

            WeatherSystemBase = api.ModLoader.GetModSystem<WeatherSystemBase>();
           
            base.OnLoaded(api);
        }

        public bool LightState(ItemStack stack)
        {
            return stack.Attributes.GetBool("togglelight");
        }

        public virtual void SetLightActive(ItemSlot slot, bool active, EntityPlayer player)
        {
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;

            bool hasFuel = collObj.GetCollectibleInterface<IPowerSource>()?.HasPower(stack) ?? false;

            // Only block activation if no fuel
            if (active && !hasFuel && WearableLightConfigHelper.NeedFuel(stack.Collectible.Code))
            {
                if (api?.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(
                        this,
                        "lightnofuel",
                        Lang.Get(
                            "awearablelight:ingameerror-light-nofuel",
                            stack.Collectible.GetHeldItemName(stack)
                        )
                    );
                }


                return;
            }

            // Play toggle sound
            bool value = AWearableLightCore.ClientConfig?.EnableSounds ?? false;

            if (value && player != null)
            {
                string soundPath = attachmentableLightProp?.soundPath ?? string.Empty;

                if (!string.IsNullOrEmpty(soundPath))
                {
                    player.World.PlaySoundAt(
                        new AssetLocation(soundPath),
                        player.Pos.X + 0.5,
                        player.Pos.Y + 0.75,
                        player.Pos.Z + 0.5,
                        null,
                        randomizePitch: false,
                        volume: 1f
                    );
                }
            }

            // Update state
            stack.Attributes.SetBool("togglelight", active);

            string currentCode = stack.Collectible.Code.ToString();

            string newCode = active
                ? currentCode.Replace("-off", "-on")
                : currentCode.Replace("-on", "-off");

            Item? newItem = api.World.GetItem(new AssetLocation(newCode));

            if (newItem != null)
            {
                ITreeAttribute clonedAttributes = stack.Attributes.Clone();

                slot.Itemstack = new ItemStack(newItem)
                {
                    Attributes = clonedAttributes
                };
            }

            slot.MarkDirty();
        }

        public virtual bool GetWeather(EntityAgent entity, ItemStack itemStack)
        {
            if (api == null) return false;

            if (WeatherSystemBase == null) return false;

            if (!LightState(itemStack)) return false;

            var pos = entity.Pos;

            float rain = WeatherSystemBase.GetPrecipitation(pos.XYZ);

            bool exposedToRain = api.World.BlockAccessor.GetRainMapHeightAt(pos.AsBlockPos.X, pos.AsBlockPos.Z) <= entity.Pos.Y;

            if (!exposedToRain || rain <= 0.4f)
            {
                return false;
            }

           double chance = GameMath.Clamp(attachmentableLightProp?.chanceExtinguish ?? 0, 0, 100 / 100f);

           return api.World.Rand.NextDouble() <= chance;
        }

        public virtual bool InLiquid(EntityAgent entity, ItemStack itemStack, EnumCharacterDressType? dressType)
        {
            if (api == null)
            {
                return false;
            }

            if (!LightState(itemStack))
            {
                return false;
            }

            // Swimming always extinguishes
            if (entity.Swimming)
            {
                return true;
            }

            var attachmentableLight =
                attachmentableLightProp;

            float offsetY =
                attachmentableLight?.extinguishOffsetY ?? 0f;

            // =====================================================
            // WATER BLOCK
            // =====================================================

            Block fluidBlock = api.World.BlockAccessor.GetBlock(entity.Pos.AsBlockPos, BlockLayersAccess.Fluid);

            if (fluidBlock == null)
            {
                return false;
            }

            int liquidLevel =
                fluidBlock.LiquidLevel;

            if (liquidLevel <= 0)
            {
                return false;
            }

            // =====================================================
            // ENTITY HEIGHT CHECK
            // =====================================================

            float entityHeight = entity.SelectionBox.YSize;

            // Deep enough to cover small entity
            if (liquidLevel >= entityHeight * 7f)
            {
                return true;
            }

            // =====================================================
            // HEAD ITEMS SURVIVE SHALLOW WATER
            // =====================================================

            if (dressType == EnumCharacterDressType.Head && liquidLevel < 7)
            {
                return false;
            }

            // =====================================================
            // WEARABLE CHECK
            // =====================================================

            return liquidLevel >= offsetY;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[2]
            {
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-toggle-activeslot"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "toggleLight"
                },
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-toggle-gearslot"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "toggleHoveredGearLight"

                }
            }.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }
    }
}