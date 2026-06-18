using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AWearableLight.Util
{
    public class FuelProps
    {
        public string PowerType { get; set; } = "fuel"; // fuel / windup / solar
        public bool UseFuel { get; set; } = false;
        public bool RainExtinguish { get; set; } = false;
        public bool InLiquid { get; set; } = false;
        public bool OnCraftedFueled { get; set; } = false;
        public float Capacity { get; set; } = 24f;
        public int[] LightHsv { get; set; } = new int[3];
        public Dictionary<string, float> FuelList { get; set; } = new();
    }
}
