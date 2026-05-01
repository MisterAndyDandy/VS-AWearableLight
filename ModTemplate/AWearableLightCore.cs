using AWearableLight.Config;
using ProtoBuf;
using System;
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
        private static readonly string ConfigName = "AwearableLight.json";

        public static ServerConfig? Config;

        private double lastServerTotalHours;
        private long OnLongServerTick { get; set; } = 0;

        private ICoreServerAPI? Sapi { get; set; }

        private IClientNetworkChannel? _clientToggleChannel;
        private IServerNetworkChannel? _serverToggleChannel;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            LoadOrCreateConfig(api);
            JsonConfig(api);
        }

        private void LoadOrCreateConfig(ICoreAPI api)
        {
            var logger = api.Logger;

            try
            {
                Config = api.LoadModConfig<ServerConfig>(ConfigName) ?? new ServerConfig();
                api.StoreModConfig(Config, ConfigName);
                logger?.VerboseDebug($"Config file '{ConfigName}' loaded or created successfully.");
            }
            catch (Exception e)
            {
                logger?.Error($"Failed to load config file '{ConfigName}'. Error: {e.Message}");
                Config = new ServerConfig();
                api.StoreModConfig(Config, ConfigName);
                logger?.Error("A new config file with default values has been created.");
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterCollectibleBehaviorClass("awearablelight:AttachmentableLight", typeof(CollectibleBehaviorAttachmentableLight));
            api.RegisterCollectibleBehaviorClass("awearablelight:Fuel", typeof(CollectibleBehaviorFuel));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            _clientToggleChannel = api.Network.RegisterChannel("awearableLight")
                .RegisterMessageType<TogglePacket>();

            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-activeslot-description"), GlKeys.L);
            api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

            api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("awearablelight:keybind-gearslot-description"), GlKeys.L, HotkeyType.CharacterControls, false, true, false);
            api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            Sapi = api;

            _serverToggleChannel = api.Network.RegisterChannel("awearableLight")
            .RegisterMessageType<TogglePacket>()
            .SetMessageHandler<TogglePacket>(ToggleWearableItem);


            OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 1000, 200);

        }

        public override void Dispose()
        {
            base.Dispose();

            Sapi?.Event.UnregisterGameTickListener(OnLongServerTick);
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

                    IFuelType item = stack.Collectible.GetCollectibleInterface<IFuelType>();

                    if(item == null) continue;

                    // Only look for items that use fuel.
                    if (!item.UseFuel(stack)) continue;

                    // Only active lights drain
                    if (!stack.Attributes.GetAsBool("togglelight")) continue;

                    // Optional future fuel behavior

                    double fuel = item.GetFuel(stack);

                    var light = stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true);

                    // Auto shutoff
                    if (fuel <= 0)
                    {
                        item.SetFuel(stack, 0);
                        light.SetLightActive(slot, false, player.Entity);
                        slot.MarkDirty();
                        continue;
                    }

                    // Drain fuel
                    item.ConsumeFuel(stack, elapsedHours);

                    // Clamp after drain
                    if (item.GetFuel(stack) <= 0)
                    {
                        item.SetFuel(stack, 0);
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
            _clientToggleChannel?.SendPacket(new TogglePacket
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
            if (Config == null) return;

            api.World.Config.GetBool("EnableSound", Config.EnableSound);
        }
    }
}
