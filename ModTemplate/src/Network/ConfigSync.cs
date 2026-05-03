using AWearableLight.Config;
using AWearableLight.Util;
using ProtoBuf;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace AWearableLight.Network
{
    #nullable enable

    [ProtoContract]
    public class ServerConfigPacket
    {
        [ProtoMember(1)]
        public List<FuelTypePacket> FuelTypes = new();
    }

    [ProtoContract]
    public class FuelTypePacket
    {
        [ProtoMember(1)]
        public string Code = string.Empty;

        [ProtoMember(2)]
        public bool UseFuel;

        [ProtoMember(3)]
        public string PowerType = "fuel";

        [ProtoMember(4)]
        public float Capacity;

        [ProtoMember(5)]
        public Dictionary<string, float> FuelList = new();
    }

    public class ConfigSyncSystem
    {
        private IServerNetworkChannel? serverChannel;
        private IClientNetworkChannel? clientChannel;

        private Server? Config;
        private ICoreAPI api;

        public ConfigSyncSystem(ICoreAPI api, Server? config)
        {
            this.api = api;
            this.Config = config;

            if (api.Side == EnumAppSide.Server)
            {
                serverChannel = (api as ICoreServerAPI)?.Network
                    .RegisterChannel("awearablelight_config")
                    .RegisterMessageType<ServerConfigPacket>();
            }

            if (api.Side == EnumAppSide.Client)
            {
                clientChannel = (api as ICoreClientAPI)?.Network
                    .RegisterChannel("awearablelight_config")
                    .RegisterMessageType<ServerConfigPacket>()
                    .SetMessageHandler<ServerConfigPacket>(OnConfigReceived);
            }
        }

        public void SendToPlayer(IServerPlayer player)
        {
            if (serverChannel == null || Config == null) return;

            var packet = new ServerConfigPacket();

            foreach (var entry in Config.FuelTypes)
            {
                packet.FuelTypes.Add(new FuelTypePacket
                {
                    Code = entry.Key,
                    UseFuel = entry.Value.UseFuel,
                    PowerType = entry.Value.PowerType,
                    Capacity = entry.Value.Capacity,
                    FuelList = entry.Value.FuelList
                });
            }

            serverChannel.SendPacket(packet, player);
        }

        private void OnConfigReceived(ServerConfigPacket packet)
        {
            var capi = api as ICoreClientAPI;

            capi?.Logger.Notification("[WearableLight] Config synced from server");

            var config = new Server
            {
                FuelTypes = new Dictionary<string, FuelProps>()
            };

            foreach (var entry in packet.FuelTypes)
            {
                config.FuelTypes[entry.Code] = new FuelProps
                {
                    UseFuel = entry.UseFuel,
                    PowerType = entry.PowerType,
                    Capacity = entry.Capacity,
                    FuelList = entry.FuelList ?? new()
                };
            }

            // Init helper
            WearableLightConfigHelper.Init(config);

            // Refresh UI (IMPORTANT)
            capi?.World.Player?.InventoryManager.BroadcastHotbarSlot();
        }
    }
}
  
