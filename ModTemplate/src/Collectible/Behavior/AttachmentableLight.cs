using AWearableLight.Config;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace AWearableLight
{

    #nullable enable

    class CollectibleBehaviorAttachmentableLight : CollectibleBehavior
    {
        private JsonObject? attSound;

        private ServerConfig? serverConfig;

        private IFuelType? fuelType => collObj.GetCollectibleInterface<IFuelType>();

        private ICoreAPI? api {  get;  set; }

        public CollectibleBehaviorAttachmentableLight(CollectibleObject collObj) : base(collObj)
        {
           
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;

            serverConfig = api.LoadModConfig<ServerConfig>("AwearableLight.json");

            attSound = collObj.Attributes["sound"];

            base.OnLoaded(api);
        }

        public bool LightState(ItemStack stack)
        {
            return stack.Attributes.GetBool("togglelight");
        }

        public void SetLightActive(ItemSlot slot, bool active, EntityPlayer? player = null)
        {
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;

            bool hasFuel = fuelType?.HasFuel(stack) ?? false;

            // Only block activation if no fuel
            if (active && fuelType != null && !hasFuel && fuelType.UseFuel(stack))
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

            // Already in correct state
            if (LightState(stack) == active) return;

            // Play toggle sound
            if (serverConfig?.EnableSound == true && player != null)
            {
                string soundPath = attSound?.AsString() ?? string.Empty;

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

            player?.Player?.InventoryManager.BroadcastHotbarSlot();
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