using System.Collections.Generic;

namespace AWearableLight.Util
{
    public class FuelProps
    {
        public bool UseFuel = true;
        public float Capacity = 24f;
        public Dictionary<string, float> FuelList = new();
        public string PowerType = "fuel"; // fuel / windup / solar
    }
}
