using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AWearableLight.Config
{
    public class ServerConfig
    {
        public bool EnableSound { get; set; } = true;

        public bool EnableItem { get; set; } = true;

        public bool EnableRecipe { get; set; } = true;

        public bool EnableTrader { get; set; } = true;   
    }
}
