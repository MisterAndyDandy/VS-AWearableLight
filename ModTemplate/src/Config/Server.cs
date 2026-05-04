using AWearableLight.Config;
using AWearableLight.Util;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace AWearableLight.Config
{
    public class Server
    {
        public int Version { get; protected set; } = 1;

        public Dictionary<string, LightProps> ItemTypes = new()
        {
            {
                "awearablelight:clothes-head-torch-*",
                new LightProps
                {
                    UseFuel = false,
                    PowerType = "windup",
                    FuelList = new(),
                    Capacity = 300f
                }
            },
            {
                "awearablelight:clothes-head-candle*",
                new LightProps
                {
                    UseFuel = true,
                    PowerType = "fuel",
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    },
                    Capacity = 24f
                }
            },
            {
                "awearablelight:clothes-head-oil-lamp-*",
                new LightProps
                {
                    UseFuel = true,
                    PowerType = "fuel",
                    FuelList = new()
                    {
                        { "game:fat-rendered", 9f },
                        { "game:fat", 4f }
                    },
                    Capacity = 30f
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-brass-lantern-*",
                new LightProps
                {
                    UseFuel = true,
                    PowerType = "fuel",
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    },
                    Capacity = 64f
                }
            }
        };
    }
}
