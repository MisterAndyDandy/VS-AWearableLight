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
                    LightHsv = [10,5,12],
                    Capacity = 300f
                }
            },
            {
                "awearablelight:clothes-head-candle1-*",
                new FuelProps
                {           
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = true,
                    OnCraftedFueled = true,
                    Capacity = 24f,
                    LightHsv = [9,3,4],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-head-candle2-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = true,
                    OnCraftedFueled = true,
                    Capacity = 24f,
                    LightHsv = [9,3,4],
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
                    LightHsv = [4,2,9],
                    FuelList = new()
                    {
                        { "game:fat-rendered", 9f },
                        { "game:fat", 4f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-brass-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-tinbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-blackbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-iron-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-copper-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-gold-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-bismuthbronze-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            },
            {
                "awearablelight:clothes-waist-sturdy-belt-silver-lantern-*",
                new FuelProps
                {
                    PowerType = "fuel",
                    UseFuel = true,
                    InLiquid = true,
                    RainExtinguish = false,
                    OnCraftedFueled = true,
                    Capacity = 64f,
                    LightHsv = [6,4,12],
                    FuelList = new()
                    {
                        { "game:candle", 8f }
                    }
                }
            }
        };
    }
}
