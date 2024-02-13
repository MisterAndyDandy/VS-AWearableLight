using CommonLib.Config;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AWearableLight
{
    [Config("AWearableLight.json")]
    public class Config
    {

        [ClientOnly]
        [Description("If you set it to false, it will disable sounds for items but only for client side (aka you)")]
        public bool Sound { get; set; } = true;

        [Description("If you set it to false, it will disable recipes only for (A Wearable Light)")]
        public bool Recipes { get; set; } = true;

        [Description("If you set it to false, it will disable items only for (A Wearable Light)")]
        public bool Items { get; set; } = true;

        [Description("If you set it to false, it will disable trader-able only for (A Wearable Light)")]
        public bool Tradable { get; set; } = true;
    }
}
