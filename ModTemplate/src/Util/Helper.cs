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

        public static FuelProps GetProps(AssetLocation code)
        {
            if (config?.FuelTypes == null) return new FuelProps();

            foreach (var entry in config.FuelTypes)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;

                if (WildcardUtil.Match(new AssetLocation(entry.Key), code))
                {
                    return entry.Value;
                }
            }

            return new FuelProps();
        }

        public static bool NeedFuel(AssetLocation code)
        {
            if (config?.FuelTypes == null) return true;

            return GetProps(code).UseFuel;
        }
    }

}
