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
    public sealed class TogglePacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }
    }

    public class AWearableLightCore : ModSystem
    {
        private static readonly string ConfigServerName = "awearablelight-server.json";

        private static readonly string ConfigClientName = "awearablelight-client.json";

        public static Server? ServerConfig;

        public static Client? ClientConfig;

        private double lastServerTotalHours;

        private long OnLongServerTick { get; set; } = 0;

        private ICoreServerAPI? Sapi { get; set; }


        private IClientNetworkChannel? clientToggleChannel;

        private IServerNetworkChannel? serverToggleChannel;

        private ConfigSyncSystem? configSync;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Server)
            {
                ServerConfig = new LoadOrCreate().SapiConfig(api, ConfigServerName);
                WearableLightConfigHelper.Init(ServerConfig);
            }

            // ❌ remove client config loading

            configSync = new ConfigSyncSystem(
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

            ClientConfig = new LoadOrCreate().CapiConfig(api, ConfigClientName);

            clientToggleChannel = api.Network
            .RegisterChannel("awearablelighttoggle")
            .RegisterMessageType<TogglePacket>();


            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-activeslot-description"), GlKeys.L);
            api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

            api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("awearablelight:keybind-gearslot-description"), GlKeys.L, HotkeyType.CharacterControls, false, true, false);
            api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));

            JsonConfig(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            Sapi = api;

            serverToggleChannel = api.Network
            .RegisterChannel("awearablelighttoggle")
            .RegisterMessageType<TogglePacket>()
            .SetMessageHandler<TogglePacket>(ToggleWearableItem);

            api.Event.PlayerNowPlaying += (player) =>
            {
                configSync?.SendToPlayer(player);
            };

            OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 1000, 200);
        }

        public static void MigrateConfig(ICoreAPI api)
        {
            if (ServerConfig == null) return;

            // Example: ensure FuelTypes exists
            ServerConfig.FuelTypes ??= new();

            api.StoreModConfig(ServerConfig, ConfigServerName);

            api.Logger.Notification($"[WearableLight] Config migrated to version {ServerConfig.Version}.");

        }

        public override void Dispose()
        {
            base.Dispose();

            Sapi?.Event.UnregisterGameTickListener(OnLongServerTick);

            configSync = null;
            ServerConfig = null;
            ClientConfig = null;
        }

        private void OnServerTick(float dt)
        {
            if (Sapi == null) return;

            double totalHours = Sapi.World.Calendar.TotalHours;
            double elapsedHours = totalHours - lastServerTotalHours;

            if (elapsedHours <= 0.05) return;

            IPlayer[] allOnlinePlayers = Sapi.World.AllOnlinePlayers;
            int count = allOnlinePlayers.Length;

            for (int i = 0; i < count; i++)
            {
                IPlayer player = allOnlinePlayers[i];

                if (player?.Entity == null) continue;

                IInventory characterInv = player.InventoryManager.GetOwnInventory("character");

                if (characterInv == null) continue;

                for (int slotId = 0; slotId < characterInv.Count; slotId++)
                {
                    ItemSlot slot = characterInv[slotId];

                    if (slot == null || slot.Empty) continue;

                    ItemStack stack = slot.Itemstack;

                    IPowerSource powerType = stack.Collectible.GetCollectibleInterface<IPowerSource>();

                    if(powerType == null) continue;

                    // Only look for items that use fuel.
                    if (!WearableLightConfigHelper.NeedFuel(stack.Collectible.Code)) continue;

                    // Only active lights drain
                    if (!stack.Attributes.GetAsBool("togglelight")) continue;

                    // Optional future fuel behavior

                    double fuel = powerType.GetPower(stack);

                    var light = stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

                    // Auto shutoff
                    if (fuel <= 0)
                    {
                        powerType.SetPower(stack, 0);
                        light.SetLightActive(slot, false, player.Entity);
                        slot.MarkDirty();
                        continue;
                    }

                    // Drain fuel
                    powerType.ConsumePower(stack, elapsedHours);

                    // Clamp after drain
                    if (powerType.GetPower(stack) <= 0)
                    {
                        powerType.SetPower(stack, 0);
                        light.SetLightActive(slot, false, player.Entity);
                    }

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

            // Gear slot override
            if (itemslot != -1 && invGear != null)
            {
                currentSlot = invGear[itemslot];
            }

            if (currentSlot == null || currentSlot.Empty) return false;

            if (currentSlot.Itemstack.Collectible
                .GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true)
                is not CollectibleBehaviorAttachmentableLight attachmentableLight)
            {
                return false;
            }

            // Toggle state
            bool newState =! attachmentableLight.LightState(currentSlot.Itemstack);

            attachmentableLight.SetLightActive(
                currentSlot,
                newState,
                player.Entity
            );

            // Sync to server
            clientToggleChannel?.SendPacket(new TogglePacket
            {
                ItemSlot = itemslot
            });

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

            if (itemslot == -1) return false;

            return ToggleWearableItem(player, itemslot);
        }

        public void ToggleWearableItem(IServerPlayer player, TogglePacket packet) => ToggleWearableItem(player, packet.ItemSlot);

        private void JsonConfig(ICoreAPI api)
        {
            if (ClientConfig == null) return;

            api.World.Config.GetBool("EnableSound", ClientConfig.EnableSounds);
        }
    }
}
