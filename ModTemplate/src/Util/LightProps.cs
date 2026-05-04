using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace AWearableLight.Util
{
    public class LightProps
    {
        public bool UseFuel = true;
        public float Capacity = 24f;
        public string PowerType = "fuel"; // fuel / windup / solar
        public Dictionary<string, float> FuelList = new();
    }
}
