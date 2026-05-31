using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AWearableLight.Util
{
    #nullable enable

    public class AttachmentableLightProp
    {
        public string soundPath { get; set; } = string.Empty;

        public float warmthBouse { get; set; } = 0f;

        public float extinguishOffsetY { get; set; } = 0;

        public double chanceExtinguish { get; set; } = 0;

        public ParticleProp? particleProp { get; set; }

        public static AttachmentableLightProp? ReadFrom(ItemStack itemStack)
        {
            if (itemStack == null) return null;

            AttachmentableLightProp? attachmentableLightProp = itemStack.Collectible.Attributes?["attachmentableLightProp"]?.AsObject<AttachmentableLightProp>();

            if (attachmentableLightProp == null)
            {
                return null;
            }


            return attachmentableLightProp;
        }

        public static AttachmentableLightProp? ReadFrom(CollectibleObject colObj)
        {
            if (colObj == null)
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
            public Vec3d? BasePos { get; set; }

            public AdvancedParticleProperties[] Particle { get; set; } = Array.Empty<AdvancedParticleProperties>();

        }
    }

}
