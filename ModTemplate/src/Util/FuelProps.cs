using System.Collections.Generic;

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
        public Dictionary<string, float> FuelList { get; set; } = new();
    }
}
