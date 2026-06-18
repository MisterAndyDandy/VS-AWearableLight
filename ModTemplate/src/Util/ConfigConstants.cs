using AWearableLight.Config;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;

namespace AWearableLight.Util
{
    public static class ConfigConstants
    {
        public static string ConfigServerName = "awearablelight-server.json";

        public static string ConfigClientName = "awearablelight-client.json";

        public static Server ServerConfig { get; set; } = null;

        public static Client ClientConfig { get; set; } = null;

    }
}
