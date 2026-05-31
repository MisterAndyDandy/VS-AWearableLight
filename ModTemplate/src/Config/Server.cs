using AWearableLight.Util;
using System.Collections.Generic;

namespace AWearableLight.Config
{
    public class Server
    {
        public int Version { get; set; } = 2;

        public Dictionary<string, FuelProps> ItemTypes { get; set; } = new()
        {
            {
                "awearablelight:clothes-head-torch-*",
                new FuelProps
                {
                   
                    PowerType = "windup",
                    UseFuel = false,       
                    InLiquid = false,
                    RainExtinguish = false,
                    OnCraftedFueled = false,
                    Capacity = 300f
                }
            },
            {
                "awearablelight:clothes-head-candle*",
                new FuelProps
                {           
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = true,
                    OnCraftedFueled = true,
                    Capacity = 24f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-head-oil-lamp-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = true,
                    OnCraftedFueled = true,
                    Capacity = 30f,
                    FuelList = new()
                    {
                        { "game:fat-rendered", 9f },
                        { "game:fat", 4f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-brass-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-tinbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-blackbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-iron-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-copper-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-gold-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-bismuthbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-heavy-belt-silver-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            }
        };
    }
}
