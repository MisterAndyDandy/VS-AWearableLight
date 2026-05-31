using AWearableLight.Config;
using AWearableLight.Interfaces;
using AWearableLight.Network;
using AWearableLight.Util;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace AWearableLight
{
    #nullable enable

    public class AWearableLightCore : ModSystem
    {
        private static readonly string ConfigServerName = "awearablelight-server.json";

        private static readonly string ConfigClientName = "awearablelight-client.json";

        public static Server? ServerConfig { get; set; }

        public static Client? ClientConfig { get; set; }

        private double lastServerTotalHours;

        public long OnLongRefreshTick {  get; set; }
        public long OnLongServerFuelTick { get; set; }
        public long OnLongServerTick { get; set; }

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

                if(WearableLightConfigHelper.oldVersion >= ServerConfig.Version)
                {
                    WearableLightConfigHelper.MigrateConfig(api);
                }
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

            api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("awearablelight:keybind-gearslot-description"), GlKeys.L, HotkeyType.GUIOrOtherControls, false, true, false);
            api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));

            api.Event.PlayerJoin += ErrorMessage;

            //foreach (var recipe in api.World.GridRecipes)
            //{
            //    if (recipe?.Output?.ResolvedItemStack?.Collectible?.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true) != null) 
            //    {
            //        var ingredient = recipe?.ResolvedIngredients?[0];
                    
            //        if (ingredient == null) continue;

            //        api.Logger.Warning(
            //            $"{recipe?.Name} => {ingredient.Tags}", ingredient.Tags.IsEmpty == true
            //        );
            //    }
            //}

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

            OnLongServerFuelTick = api.Event.RegisterGameTickListener(OnServerFuelTick, 2000);
            OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 100);

        }

        private void OnServerTick(float obj)
        {
            if (Sapi == null) return;

            foreach (IServerPlayer player in Sapi.World.AllOnlinePlayers)
            {
                if (player?.Entity == null) continue;

                var invGear =
                    player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

                if (invGear == null) continue;

                foreach (ItemSlot slot in invGear)
                {
                    if (slot == null || slot.Empty) continue;

                    var item = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

                    if (item == null) continue;

                    var prop = WearableLightConfigHelper.GetProps(item.collObj.Code);

                    if(prop.InLiquid || prop.RainExtinguish) continue;

                    if (!WearableLightConfigHelper.NeedFuel(
                        slot.Itemstack.Collectible.Code))
                    {
                        continue;
                    }

                    var dresstype = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorWearable>(true)?.GetDressType(slot);

                    if (item.InLiquid(player.Entity, slot.Itemstack, dresstype) || item.GetWeather(player.Entity, slot.Itemstack))
                    {
                        ToggleWearableItem(player, new AWearableLightPacket() { ItemSlot = invGear.GetSlotId(slot) });
                    }
                }
            }
        }

        private void OnServerFuelTick(float dt)
        {
            if (Sapi == null) return;

            double totalHours = Sapi.World.Calendar.TotalHours;
            double hoursPassed = totalHours - lastServerTotalHours;

            hoursPassed = Math.Min(hoursPassed, 0.5);

            if (hoursPassed <= 0)
            {
                lastServerTotalHours = totalHours;
                return;
            }

            foreach (IServerPlayer player in Sapi.World.AllOnlinePlayers)
            {
                if (player != null)
                {
                    var invGear =
                    player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

                    if (invGear == null) continue;

                    foreach (ItemSlot slot in invGear)
                    {
                        if (slot == null || slot.Empty) continue;

                        var source = slot.Itemstack.Collectible.GetCollectibleInterface<IPowerSource>();

                        if (source == null) continue;

                        if (!WearableLightConfigHelper.NeedFuel(
                            slot.Itemstack.Collectible.Code))
                        {
                            continue;
                        }

                        source.ConsumePower(
                            slot,
                            player.Entity,
                            hoursPassed
                        );

                        slot.MarkDirty();
                    }
                }
            }

            lastServerTotalHours = totalHours;
        }

        public override void Dispose()
        {
            base.Dispose();

            Sapi?.Event.UnregisterGameTickListener(OnLongServerFuelTick);
            Sapi?.Event.UnregisterGameTickListener(OnLongRefreshTick);
            Sapi?.Event.UnregisterGameTickListener(OnLongServerTick);

            ConfigSync = null;
            ServerConfig = null;
            ClientConfig = null;
        }

        private bool ToggleWearableItem(IPlayer player, int itemslot = -1)
        {
            if (player == null) return false;

            var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

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

            var newState =! attachmentableLight.LightState(currentSlot.Itemstack);

            attachmentableLight.SetLightActive(currentSlot, newState, player.Entity);

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
            var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

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
