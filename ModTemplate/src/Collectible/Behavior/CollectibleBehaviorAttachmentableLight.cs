using AWearableLight.Config;
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

    public class AttachmentableLightProp
    {
        public string soundPath { get; set; } = string.Empty;

        public float warmthBouse { get; set; } = 0f;

        public ParticleProp? particleProp { get; set; }

        //public RelaxedReadOnlyDictionary<string, ParticleProp>? particleProps { get; set; }

        public static AttachmentableLightProp? ReadFrom(ItemStack itemStack)
        {
            if(itemStack == null) return null;

            AttachmentableLightProp? attachmentableLightStats = itemStack.Collectible.Attributes?["attachmentableLightProp"]?.AsObject<AttachmentableLightProp>();

            if (attachmentableLightStats == null)
            {
                return null;
            }

            return attachmentableLightStats;
        }

        public static AttachmentableLightProp? ReadFrom(CollectibleObject colObj)
        {
            if(colObj == null)
            {
                return null;
            }

            AttachmentableLightProp? attachmentableLightProp = colObj.Attributes?["attachmentableLightProp"]?.AsObject<AttachmentableLightProp>();

            if (attachmentableLightProp == null)
            {
                return null;
            }

            return attachmentableLightProp; 
        }


        public class ParticleProp 
        {
            public string Colour { get; set; } = string.Empty;

            public Vec3d AttachmentPoint { get; set; } = new Vec3d();

        }
    }


    class CollectibleBehaviorAttachmentableLight : CollectibleBehavior
    {
        private IPowerSource? powerType => collObj.GetCollectibleInterface<IPowerSource>();

        private ICoreAPI? api { get; set; }

        private Client? ClientConfig { get; set; }

        public AttachmentableLightProp? attachmentableLightProp => AttachmentableLightProp.ReadFrom(this.collObj);

        public AdvancedParticleProperties? advancedParticle 
        {
            get; 
            set;
        } = new AdvancedParticleProperties();

        public CollectibleBehaviorAttachmentableLight(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;

            if (api.Side == EnumAppSide.Client)
            {
                ClientConfig = new LoadOrCreate().CapiConfig(api, "awearablelight-client.json");
            }

            advancedParticle?.Init(api);

            _ = collObj.ParticleProperties;

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

            bool hasFuel = powerType?.HasPower(stack) ?? false;

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

            // Already in correct state
            if (LightState(stack) == active) return;

            // Play toggle sound
            bool value = ClientConfig?.EnableSounds ?? true;

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