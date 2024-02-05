using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using AWearableLight.Util;
using System.Runtime.CompilerServices;
using Vintagestory.API.Util;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods.NoObf;
using System.Configuration;
using Vintagestory.API.Server;
using HarmonyLib;

namespace AWearableLight.Util
{
    public class Config
    {
        public static Config Current { get; set; }

        public class Part<T>
        {
            public readonly string Comment;
            public readonly T Default;
            private T val;
            public T Value
            {
                get { return val != null ? val : val = Default; }
                set { val = value != null ? value : Default; }
            }
            public Part(T Default, string Comment = null)
            {
                this.Default = Default;
                this.Value = Default;
                this.Comment = Comment;
            }
            public Part(T Default, string prefix, string[] allowed, string postfix = null)
            {
                this.Default = Default;
                this.Value = Default;
                this.Comment = prefix;

                this.Comment += "[" + allowed[0];
                for (int i = 1; i < allowed.Length; i++)
                {
                    this.Comment += ", " + allowed[i];
                }
                this.Comment += "]" + postfix;
            }
        }

        public Part<bool> DisableItem { get; set; }
        public Part<bool> DisableRecipes { get; set; }
        public Part<bool> DisableSound { get; set; }
        public Part<bool> DisableTradable { get; set; }
        public Part<bool> TemporalTinkerer { get; set; }

        public Config()
        {
           
            DisableItem = new Part<bool>(false, "Removing items from game");
            DisableRecipes = new Part<bool>(false, "Removing items recipes from game");
            DisableSound = new Part<bool>(false, "On/Off sounds for items when being turn on and off.");
            DisableTradable = new Part<bool>(false, "Rmove tradable items from traders");
            TemporalTinkerer = new Part<bool>(false, "Set it to true if you want to it enable");
        }
    }
}
