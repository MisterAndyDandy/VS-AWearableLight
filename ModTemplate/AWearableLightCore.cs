using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using CommonLib.Config;
using Vintagestory.ServerMods.NoObf;
using System;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace AWearableLight
{
    class AWearableLightCore : ModSystem
    {

        Harmony harmony = new Harmony("com.misterandydandy.a.wearable.light");

        public static Config Config { get; private set; } = null!;
        public static string ModId { get; private set; }
        public static ILogger ModLogger { get; private set; }

        public static ICoreClientAPI capi { get; private set; }

        public static ICoreServerAPI sapi { get; private set; }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            ModId = Mod.Info.ModID;
            ModLogger = Mod.Logger;

            if (api.Side == EnumAppSide.Client)
            {
                capi = api as ICoreClientAPI;
            }
            else {
                sapi = api as ICoreServerAPI;   
            }

        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("AttachmentableLight", typeof(ItemAttachmentableLight));
            api.RegisterCollectibleBehaviorClass("CollectibleBagsBehavior", typeof(CollectibleBagsBehavior));

            Config = api.ModLoader.GetModSystem<ConfigManager>().GetConfig<Config>();

            if (Config != null)
            {
                JsonConfig(api, Config);
            }

            api.Logger.Event("I see " + Mod.Info.Name.UcFirst());

  
            ModCompatibility(api);

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            sapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;
        
            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-description"), GlKeys.L, HotkeyType.CharacterControls, false, false, false);
            api.Input.SetHotKeyHandler("toggleLight", (KeyCombination _) => LightToggle());
        }

        private void ModCompatibility(ICoreAPI api)
        {
            foreach (Mod modCompat in api.ModLoader.Mods)
            {
                string name = modCompat.Info.ModID;

                List<AssetLocation> assetLocations = api.Assets.GetLocations("patches/compatibility/" + name + "/" + name, ModId);             
                
                bool matchedmod = assetLocations.Count > 0;

                if (matchedmod)
                {
                    if (api.ModLoader.IsModEnabled(name))
                    {
                        api.World.Config.SetBool(name.UcFirst(), true);
                    }
                }
            }
        }

        private bool LightToggle()
        {
            if (sapi != null || capi != null) 
            {
                if (capi.World.Player != null)
                {
                    IPlayer player = sapi.World.PlayerByUid(capi.World.Player.PlayerUID);

                    if (player != null)
                    {
                        ItemSlot ActiveHand = player.InventoryManager.ActiveHotbarSlot;

                        if(ActiveHand.Empty) return false;

                        if (ActiveHand.Itemstack.Collectible is ItemAttachmentableLight attachmentableLight)
                        {
                            attachmentableLight.OnUsedBy(ActiveHand, player.Entity);
                           
                        }
                    }
                }
            }

            return true;
        }

        public void JsonConfig(ICoreAPI api, Config config)
        {
            api.World.Config.GetBool("Sound", config.Sound);
            api.World.Config.SetBool("Item", config.Items);
            api.World.Config.SetBool("Recipes", config.Recipes);
            api.World.Config.SetBool("Tradable", config.Tradable);

        }

        public override void AssetsFinalize(ICoreAPI api)
        {
         
            foreach (var item in api.World.Collectibles)
            {
                if (item.Attributes != null)
                {
                    if (item.Attributes["Backpack"].Exists)
                    {
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new CollectibleBagsBehavior(item));
                    }
                }
            }
    
            base.AssetsFinalize(api);
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }
    }

   
}
