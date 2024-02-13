using CommonLib.Config;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AWearableLight
{
    [Config("awearablelight.json")]
    public class Config
    {

        [ClientOnly]
        [Description("If you set it to true, it will disable sounds for items but only for client side (aka you)")]
        public bool DisableSound { get; set; } = false;

        [Description("If you set it to true, it will disable recipes only for (A Wearable Light)")]
        public bool DisableRecipes { get; set; } = false;

        [Description("If you set it to true, it will disable items only for (A Wearable Light)")]
        public bool DisableItems { get; set; } = false;

        [Description("If you set it to true, it will disable trader-able only for (A Wearable Light)")]
        public bool DisableTradable { get; set; } = false;
    }
}
