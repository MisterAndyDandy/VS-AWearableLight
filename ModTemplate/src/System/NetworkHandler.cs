using AWearableLight.Interfaces;
using AWearableLight.Network;
using AWearableLight.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AWearableLight.System
{
    #nullable enable

    public class NetworkHandler: ModSystem
    {
        public ICoreClientAPI? Capi {  get; set; }

        public IClientNetworkChannel? ClientToggleChannel { get; set; }

        public IServerNetworkChannel? ServerToggleChannel { get; set; }

        public IClientNetworkChannel? ClientFuelChannel { get; set; }

        public IServerNetworkChannel? ServerFuelChannel { get; set; }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            Capi = api;

            ClientToggleChannel = api.Network
            .RegisterChannel("awearablelighttoggle")
            .RegisterMessageType<AWearableLightPacket>();

            ClientFuelChannel = api.Network.RegisterChannel("awearablelightfuel").RegisterMessageType<FuelSyncPacket>().SetMessageHandler<FuelSyncPacket>(FuelSync);

            api.Input.RegisterHotKey("toggleLight", Lang.Get("awearablelight:keybind-activeslot-description"), GlKeys.L);
            api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

            api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("awearablelight:keybind-gearslot-description"), GlKeys.L, HotkeyType.GUIOrOtherControls, false, true, false);
            api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);


            ServerToggleChannel = api.Network
           .RegisterChannel("awearablelighttoggle")
           .RegisterMessageType<AWearableLightPacket>()
           .SetMessageHandler<AWearableLightPacket>(ToggleWearableItem);

            ServerFuelChannel = api.Network.RegisterChannel("awearablelightfuel").RegisterMessageType<FuelSyncPacket>();
        }

        public override void Dispose()
        {
            base.Dispose();

            ServerFuelChannel = null;
            ServerToggleChannel = null;
            ClientFuelChannel = null;
            ClientToggleChannel = null;
        }

        public void FuelSync(FuelSyncPacket packet)
        {
            var clientPlayer = Capi?.World?.Player;

            if (clientPlayer != null)
            {
                var slot = clientPlayer.InventoryManager.GetInventory(packet.InvClass)?[packet.IdSlot];

                if (slot == null || slot.Empty) return;

                slot.Itemstack.Collectible.GetCollectibleInterface<IPowerSource>()?.SetPower(slot.Itemstack, packet.FuelHours);
            }
        }

        public bool ToggleWearableItem(IPlayer? player, int itemslot = -1)
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

            var newState = !attachmentableLight.LightState(currentSlot.Itemstack);

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

        public bool OnToggleLightHotkey(IPlayer? player)
        {
            return ToggleWearableItem(player);
        }

        public bool OnToggleHoveredGearLightHotkey(IPlayer? player)
        {
            if (player == null) return false;

            var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

            ItemSlot? hoveredSlot = player.InventoryManager.CurrentHoveredSlot;
            int itemslot = invGear?.GetSlotId(hoveredSlot) ?? -1;

            if (itemslot == -1)
            {
                return false;
            }

            LoggerExt.SendLogger(Capi, [$"The current slot that {player.PlayerName} is hovered over: {itemslot}"]);

            return ToggleWearableItem(player, itemslot);
        }

        public void ToggleWearableItem(IServerPlayer? player, AWearableLightPacket packet) => ToggleWearableItem(player, packet.ItemSlot);
    }
}
