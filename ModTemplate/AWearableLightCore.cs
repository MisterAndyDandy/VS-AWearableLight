using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using AWearableLight.Util;
using CommonLib.Utils;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.Client;
using System;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Microsoft.VisualBasic;

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

        public GlKeys DEFAULT_KEY = GlKeys.L;

        public static ActionConsumable<KeyCombination> actionConsumable { get; private set; }

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

            var config = Config.Current = api.LoadOrCreateConfig<Config>(Mod.Info.Name.UcFirst() + ".json", ModLogger);
            JsonConfig(api, config);
         
            if (api != null)
            {
                api.Logger.Notification("// * " + this.Mod.Info.Name.ToString().UcFirst() + "Started" + " * //");
                api.Logger.Notification("I Will Light Your Way By " + this.Mod.Info.Name.ToString().UcFirst());
                api.Logger.Notification("// * " + this.Mod.Info.Name.ToString().UcFirst() + "Ended" + " * //");
            }


            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api;
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-description"), DEFAULT_KEY, HotkeyType.CharacterControls, false, false, false);
            api.Input.SetHotKeyHandler("toggleLight", (KeyCombination _) => LightToggle());
        }

       
        private bool LightToggle()
        { 
            if (sapi == null || capi == null) return false;

            IPlayer player = sapi.World.PlayerByUid(capi.World.Player.PlayerUID);

            if (player != null)
            {
                ItemSlot rightHand = player.Entity.RightHandItemSlot;

                if (rightHand.Empty) return false;

                if (rightHand.Itemstack.Collectible is ItemAttachmentableLight attachmentableLight)
                {
                    rightHand.Itemstack = new ItemStack(sapi.World.GetItem(new AssetLocation(attachmentableLight.Code.Domain, attachmentableLight.GetAssetLocation())))
                    {
                        Attributes = rightHand.Itemstack.Attributes.Clone()
                    };

                    rightHand.MarkDirty();
                    player.InventoryManager.BroadcastHotbarSlot();
                }
            }
            else
            {
                return false;
            }

            return true;
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

        public static void JsonConfig(ICoreAPI api, Config config)
        {
          
            api.World.Config.SetBool("DisableItem", config.DisableItem.Value);
            api.World.Config.GetBool("DisableSound", config.DisableSound.Value);
            api.World.Config.SetBool("DisableRecipes", config.DisableRecipes.Value);
            api.World.Config.SetBool("DisableTradable", config.DisableTradable.Value);
            api.World.Config.SetBool("TemporalTinkerer", config.TemporalTinkerer.Value);

        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }
    }
}
