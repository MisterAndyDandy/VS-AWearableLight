using AWearableLight.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AWearableLight.Util
{
    public static class WearableLightConfigHelper
    {
        private static Server config;

        public static void Init(Server cfg)
        {
            config = cfg;
        }

        public static LightProps GetProps(AssetLocation code)
        {
            if (config?.ItemTypes == null) return new LightProps();

            foreach (var entry in config.ItemTypes)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;

                if (WildcardUtil.Match(new AssetLocation(entry.Key), code))
                {
                    return entry.Value;
                }
            }

            return new LightProps();
        }

        public static bool NeedFuel(AssetLocation code)
        {
            if (config?.ItemTypes == null) return true;

            return GetProps(code).UseFuel;
        }
    }

}
