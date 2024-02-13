using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using CommonLib.Config;

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

            var configs = api.ModLoader.GetModSystem<ConfigManager>();
            Config = configs.GetConfig<Config>();

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
                bool matchedmod = api.Assets.Exists(new AssetLocation(ModId, "patches/compatibility/" + modCompat.Info.ModID + ".json"));

                if (!matchedmod) continue;

                if (matchedmod)
                {
                    if (api.ModLoader.IsModEnabled(modCompat.Info.ModID))
                    {
                        api.World.Config.SetBool(modCompat.Info.ModID.UcFirst(), true);
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

                        if (ActiveHand.Itemstack.Collectible is ItemAttachmentableLight attachmentableLight)
                        {
                            attachmentableLight.OnUsedBy(ActiveHand, player.Entity);
                          
                        }
                    }
                }
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

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }
    }
}
