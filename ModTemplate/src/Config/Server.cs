using AWearableLight.Config;
using AWearableLight.Util;
using System.Collections.Generic;

namespace AWearableLight.Config
{
    public class Server
    {
        public int Version { get; protected set; } = 1;

        public Dictionary<string, FuelProps> FuelTypes = new()
        {
            {
                "awearablelight:clothes-head-torch-*",
                new FuelProps
                {
                    UseFuel = false,
                    PowerType = "windup",
                    FuelList = new(),
                    Capacity = 300f
                }
            },
            {
                "awearablelight:clothes-head-candle*",
                new FuelProps
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
                new FuelProps
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
                new FuelProps
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
