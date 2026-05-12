using AWearableLight.Config;
using ProperVersion;
using Vintagestory.API.Client;
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
            if (config?.ItemTypes == null) return new FuelProps();

            foreach (var entry in config.ItemTypes)
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
            if (config?.ItemTypes == null) return true;

            return GetProps(code).UseFuel;
        }

        public static void MigrateConfig(ICoreAPI api)
        {
            if (config == null) return;

            // Example: ensure FuelTypes exists
            config.ItemTypes ??= new();

            api.StoreModConfig(config, "awearablelight-server.json");

            api.Logger.Notification($"[WearableLight] Config migrated to version {config.Version}.");

        }
    }

    public class LoggerExt
    {
        public static void SendLogger(ICoreClientAPI capi, string[] logs)
        {
            if (capi == null) return;

            var match = capi.Settings.Bool.Get("developerMode");

            if (match)
            {
                foreach (var log in logs)
                {
                    if (string.IsNullOrEmpty(log)) continue;

                    capi.Logger.Debug(log);

                }
            }
        }
    }

}
