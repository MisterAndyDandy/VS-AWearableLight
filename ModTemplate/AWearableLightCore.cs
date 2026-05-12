using AWearableLight.Config;
using AWearableLight.Interfaces;
using AWearableLight.Network;
using AWearableLight.Util;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AWearableLight
{
    #nullable enable

    [ProtoContract]
    public sealed class AWearableLightPacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }
    }

    public class AWearableLightCore : ModSystem
    {
        private static readonly string ConfigServerName = "awearablelight-server.json";

        private static readonly string ConfigClientName = "awearablelight-client.json";

        public static Server? ServerConfig { get; set; }

        public static Client? ClientConfig { get; set; }

        private double lastServerTotalHours;

        public long OnLongRefreshTick {  get; set; } = 0;

        public long OnLongParticleTick { get; set; } = 0;

        private long OnLongServerTick { get; set; } = 0;

        private ICoreServerAPI? Sapi { get; set; }

        private ICoreClientAPI? Capi { get; set; }

        private bool warnedDynamicLights;

        private IClientNetworkChannel? ClientToggleChannel { get; set; }

        private IServerNetworkChannel? ServerToggleChannel { get; set; }

        public ConfigSyncSystem? ConfigSync { get; set; }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Server)
            {
                ServerConfig = new LoadOrCreate().SapiConfig(api, ConfigServerName);
                WearableLightConfigHelper.Init(ServerConfig);
            }

            ConfigSync = new ConfigSyncSystem(
                api,
                api.Side == EnumAppSide.Server ? ServerConfig : null
            );

            
            api.RegisterCollectibleBehaviorClass("awearablelight:AttachmentableLight", typeof(CollectibleBehaviorAttachmentableLight));
            api.RegisterCollectibleBehaviorClass("awearablelight:Fuel", typeof(CollectibleBehaviorFuel));
            api.RegisterCollectibleBehaviorClass("awearablelight:WindUp", typeof(CollectibleBehaviorWindup));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            Capi = api;

            ClientConfig = new LoadOrCreate().CapiConfig(api, ConfigClientName);

            ClientToggleChannel = api.Network
            .RegisterChannel("awearablelighttoggle")
            .RegisterMessageType<AWearableLightPacket>();

            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-activeslot-description"), GlKeys.L);
            api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

            api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("awearablelight:keybind-gearslot-description"), GlKeys.L, HotkeyType.CharacterControls, false, true, false);
            api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));

            api.Event.PlayerJoin += ErrorMessage;

            new ParticleSystemCore(api).StartParticle();
        }

        private void ErrorMessage(IClientPlayer byPlayer)
        {
            if (warnedDynamicLights || byPlayer == null || Capi == null) return;

            int maxlights = Capi.Settings.Int["maxdynamiclights"];

            if (maxlights < 10)
            {
                warnedDynamicLights = true;

                Capi.ShowChatMessage($"[{this.Mod.Info.Name}] Max Dynamic Lights is set to {maxlights}. Some wearable lights may not be visible. Recommended: 15+");
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            Sapi = api;

            ServerToggleChannel = api.Network
            .RegisterChannel("awearablelighttoggle")
            .RegisterMessageType<AWearableLightPacket>()
            .SetMessageHandler<AWearableLightPacket>(ToggleWearableItem);

            api.Event.PlayerNowPlaying += (player) =>
            {
                ConfigSync?.SendToPlayer(player);
            };

            OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 1000, 200);

        }

        public override void Dispose()
        {
            base.Dispose();

            Sapi?.Event.UnregisterGameTickListener(OnLongServerTick);
            Sapi?.Event.UnregisterGameTickListener(OnLongRefreshTick);
            Capi?.Event.UnregisterGameTickListener(OnLongParticleTick);

            ConfigSync = null;
            ServerConfig = null;
            ClientConfig = null;
        }

        private void OnServerTick(float dt)
        {
            if (Sapi == null) return;

            double totalHours = Sapi.World.Calendar.TotalHours;
            double hoursPassed = totalHours - lastServerTotalHours;

            foreach (IPlayer? player in Sapi.World.AllOnlinePlayers)
            {
                if (player.Entity == null) continue;

                IInventory? inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

                if (inventory == null) continue;

                foreach (ItemSlot slot in inventory)
                {
                    if (slot == null || slot.Empty) continue;

                    // Skip non-fuel items
                    if (!WearableLightConfigHelper.NeedFuel(slot.Itemstack.Collectible.Code))
                    {
                        continue;
                    }

                    slot.Itemstack.Collectible.GetCollectibleInterface<IPowerSource>()?.ConsumePower(slot, player.Entity, hoursPassed);

                    slot.MarkDirty();
                }
            }

            lastServerTotalHours = totalHours;
        }

        private bool ToggleWearableItem(IPlayer player, int itemslot = -1)
        {
            if (player == null) return false;

            InventoryBase? invGear = player.Entity
                .GetBehavior<EntityBehaviorPlayerInventory>()?
                .Inventory;

            ItemSlot? currentSlot = player.InventoryManager.ActiveHotbarSlot;

            var logger = player.Entity.Api.Logger;

            // Gear slot override
            if (itemslot != -1 && invGear != null)
            {
                currentSlot = invGear[itemslot];
            }

            if (currentSlot == null || currentSlot.Empty) return false;

            var attachmentableLight = currentSlot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

            if (attachmentableLight == null) return false;

            bool newState =
             !attachmentableLight.LightState(currentSlot.Itemstack);

            attachmentableLight.SetLightActive(
                currentSlot,
                newState,
                player.Entity
            );

            currentSlot.MarkDirty();

            // Sync to server
            ClientToggleChannel?.SendPacket(
                new AWearableLightPacket
                {
                    ItemSlot = itemslot
                }
            );

            // Logging
            LoggerExt.SendLogger(
                Capi,
                [
                    $"Item: {currentSlot.Itemstack.Collectible.Code}",
                    $"Toggled: {newState}",
                    $"Player: {player.PlayerName}"
                ]
            );

            return true;
        }

        private bool OnToggleLightHotkey(IPlayer player)
        {
            return ToggleWearableItem(player);
        }

        private bool OnToggleHoveredGearLightHotkey(IPlayer player)
        {
            InventoryBase? invGear = player.Entity
                .GetBehavior<EntityBehaviorPlayerInventory>()?
                .Inventory;

            ItemSlot? hoveredSlot = player.InventoryManager.CurrentHoveredSlot;
            int itemslot = invGear?.GetSlotId(hoveredSlot) ?? -1;

            if (itemslot == -1) 
            {
                return false;
            }

            LoggerExt.SendLogger(Capi, [$"The current slot that {player.PlayerName} is hovered over: {itemslot}"] );

            return ToggleWearableItem(player, itemslot);
        }

        public void ToggleWearableItem(IServerPlayer player, AWearableLightPacket packet) => ToggleWearableItem(player, packet.ItemSlot);
    }
}
