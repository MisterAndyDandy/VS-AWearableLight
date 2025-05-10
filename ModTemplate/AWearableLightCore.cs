using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using HarmonyLib;
using Vintagestory.API.Util;
using System.Linq;
using System;
using System.Reflection;
using Vintagestory.GameContent;
using ProtoBuf;
using AWearableLight.Config;

namespace AWearableLight
{
    [ProtoContract]
    public sealed class TogglePacket
    {
        [ProtoMember(1)]
        public byte[] ItemStack { get; set; }
    }

    public class AWearableLightCore : ModSystem
    {
        private static readonly string ConfigName = "AwearableLight.json";
        public static ServerConfig Config;
        public static string ModId { get; private set; }
        public static ILogger Logger { get; private set; }

        private readonly Harmony _harmony = new("com.misterandydandy.AwearableLight");
        private IClientNetworkChannel _clientToggleChannel;
        private IServerNetworkChannel _serverToggleChannel;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            ModId = Mod.Info.ModID;
            Logger = Mod.Logger;

            LoadOrCreateConfig(api);

            JsonConfig(api);
        }

        private void LoadOrCreateConfig(ICoreAPI api)
        {
            try
            {
                Config = api.LoadModConfig<ServerConfig>(ConfigName) ?? new ServerConfig();
                api.StoreModConfig(Config, ConfigName);
                Logger.VerboseDebug($"Config file '{ConfigName}' loaded or created successfully.");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load config file '{ConfigName}'. Error: {e.Message}");
                Config = new ServerConfig();
                api.StoreModConfig(Config, ConfigName);
                Logger.Error("A new config file with default values has been created.");
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            api.RegisterItemClass("AttachmentableLight", typeof(ItemAttachmentableLight));
            api.RegisterCollectibleBehaviorClass("CollectibleBagsBehavior", typeof(CollectibleBagsBehavior));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            _clientToggleChannel = api.Network.RegisterChannel("awearableLight")
                .RegisterMessageType<TogglePacket>();

            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-description"), GlKeys.L, HotkeyType.CharacterControls);
            api.Input.SetHotKeyHandler("toggleLight", _ => ToggleWearableItem(api.World.Player));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            _serverToggleChannel = api.Network.RegisterChannel("awearableLight")
                .RegisterMessageType<TogglePacket>()
                .SetMessageHandler<TogglePacket>((player, packet) => ToggleWearableItem(player));
        }

        private bool ToggleWearableItem(IPlayer player)
        {
            if (player == null) return false;

            ItemSlot activeSlot = player.InventoryManager.ActiveHotbarSlot;

            if (activeSlot.Empty || activeSlot.Itemstack.Collectible is not ItemAttachmentableLight lightItem) return false;

            lightItem.OnUsed(activeSlot, player.Entity);

            _clientToggleChannel?.SendPacket(new TogglePacket { ItemStack = activeSlot.Itemstack.ToBytes() });

            return true;
        }

        private void JsonConfig(ICoreAPI api)
        {
            if (Config == null) return;

            api.World.Config.GetBool("EnableSound", Config.EnableSound);
            api.World.Config.SetBool("EnableItem", Config.EnableItem);
            api.World.Config.SetBool("EnableRecipe", Config.EnableRecipe);
            api.World.Config.SetBool("EnableTrader", Config.EnableTrader);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            foreach (var item in api.World.Collectibles)
            {
                if (item.Attributes?["Backpack"].Exists == true && !item.LightHsv.SequenceEqual(new byte[] { 0, 0, 0 }))
                {
                    item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new CollectibleBagsBehavior(item));
                }
            }

            base.AssetsFinalize(api);
        }

        public override void Dispose()
        {
            _harmony.UnpatchAll(_harmony.Id);
            base.Dispose();
        }
    }
}
