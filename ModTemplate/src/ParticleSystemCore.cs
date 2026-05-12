using AWearableLight.Config;
using AWearableLight.System;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AWearableLight
{
    #nullable enable

    public class ParticleSystemCore
    {
        private ICoreClientAPI? Capi { get; set; }

        public ParticleSystemCore(ICoreClientAPI api) => Capi = api;

        protected GuiDialogParticleEditor? guiDialogParticleEditor { get; set; }

        public Vec3d? offPos { get; set; }

        public void StartParticle()
        {
            if (Capi == null) return;

            Capi.Input.RegisterHotKey("particleeditor", "Particle Editor", GlKeys.P, HotkeyType.GUIOrOtherControls);

            Capi.Input.SetHotKeyHandler("particleeditor", key =>
            {
                if (guiDialogParticleEditor == null)
                {
                    guiDialogParticleEditor = new GuiDialogParticleEditor(Capi);
                }
                
                guiDialogParticleEditor.TryOpen();
                return true;
            });


            Capi.ModLoader.GetModSystem<AWearableLightCore>().OnLongParticleTick = Capi.Event.RegisterGameTickListener(OnSpawnParticles, 200, 100);
           
        }

        public void OnSpawnParticles(float obj)
        {
            if (Capi == null) return;

            var player = Capi.World.Player;

            if (player?.Entity == null) return;

            IInventory characterInv = player.InventoryManager.GetOwnInventory("character");

            if (characterInv == null) return;

            var slots = GetLightSlots(characterInv);

            foreach (var slot in slots)
            {
                if (slot.Empty) continue;

                var attachmentableLight = AttachmentableLightProp.ReadFrom(slot.Itemstack);

                if (!UseParticle(attachmentableLight)) continue;

                if (!slot.Itemstack.Attributes.GetAsBool("togglelight")) continue;

                Vec3d localOffset =
                attachmentableLight?.particleProp?.AttachmentPoint
                ?.Clone()
                ?? Vec3d.Zero;

                // Editor adjustment
                if (guiDialogParticleEditor != null)
                {
                    localOffset.Add(guiDialogParticleEditor.offset);
                }

                float yaw = player.Entity.BodyYaw;

                double cos = Math.Cos(yaw);
                double sin = Math.Sin(yaw);

                double rotatedX =
                    localOffset.X * cos -
                    localOffset.Z * sin;

                double rotatedZ =
                    -(localOffset.X * sin +
                      localOffset.Z * cos);

                Vec3d basePos = player.Entity.Pos.XYZ.AddCopy(
                    0,
                    player.Entity.LocalEyePos.Y,
                    0
                );

                Vec3d finalPos = basePos.AddCopy(
                    rotatedX,
                    localOffset.Y,
                    rotatedZ
                );

                var advancedParticleProperties = slot.Itemstack.Collectible.ParticleProperties;

                if (advancedParticleProperties == null) continue;

                // Smoke
                SimpleParticleProperties particleProperties =
                    GetParticleProperties(
                        Capi,
                        advancedParticleProperties[2].GetRgbaColor(Capi),
                        finalPos
                    );

                Capi.World.SpawnParticles(particleProperties);

                // Fire
                particleProperties.Color = advancedParticleProperties[0].GetRgbaColor(Capi);
                particleProperties.ParticleModel = EnumParticleModel.Quad;

                Capi.World.SpawnParticles(particleProperties);
            }
        }

        public bool UseParticle(AttachmentableLightProp? attachmentableLightProp)
        {
            return attachmentableLightProp?.particleProp != null;
        }

        public List<ItemSlot> GetLightSlots(IInventory inv)
        {
            List<ItemSlot> slots = new();

            for (int i = 0; i < inv.Count; i++)
            {
                ItemSlot slot = inv[i];

                if (slot == null || slot.Empty) continue;

                var stack = slot.Itemstack;

                var behavior = stack.Collectible
                    .GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

                if (behavior == null) continue;

                slots.AddIfNotPresent(slot);
            }

            return slots;
        }

        public SimpleParticleProperties GetParticleProperties(ICoreClientAPI api, int color, Vec3d pos)
        {
            return new SimpleParticleProperties
            {
               
                MinPos = pos,
                Color = color,
                MinSize = 0.1f,
                MaxSize = 0.2f,
                LifeLength = 0.2f,
                MinQuantity = 0.1f,
                ParticleModel = EnumParticleModel.Quad,
                AddQuantity = 0.5f,
                MinVelocity = new Vec3f(0f, 0.05f, 0f),
                AddVelocity = new Vec3f(0.025f, 0.25f, 0.025f)

            };
        }

    }
}
