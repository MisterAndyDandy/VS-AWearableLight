using AWearableLight.Config;
using AWearableLight.Util;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AWearableLight.Network
{
    #nullable enable

    [ProtoContract]
    public class ServerConfigPacket
    {
        [ProtoMember(1)]
        public List<ItemTypePacket> ItemTypes = new();
    }

    [ProtoContract]
    public class ItemTypePacket
    {
        [ProtoMember(1)]
        public string Code = string.Empty;

        [ProtoMember(2)]
        public string PowerType = string.Empty;

        [ProtoMember(3)]
        public bool RainExtinguish;

        [ProtoMember(4)]
        public bool InLiquid;

        [ProtoMember(5)]
        public bool UseFuel;

        [ProtoMember(6)]
        public bool OnCraftedFueled;

        [ProtoMember(7)]
        public float Capacity;

        [ProtoMember(8)]
        public int[]? LightHsv;

        [ProtoMember(9)]
        public Dictionary<string, float> FuelList = new();
    }

    public class ConfigSyncSystem
    {
        private IServerNetworkChannel? serverChannel;
        private IClientNetworkChannel? clientChannel;

        private string ConfigServerName = "awearablelight-server.json";

        private DateTime lastConfigWriteTime;
        private readonly string configPath;

        private Server? Config;
        private ICoreAPI api;

        public ConfigSyncSystem(ICoreAPI api, Server? config)
        {
            this.api = api;
            this.Config = config;

            configPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), ConfigServerName);

            if (File.Exists(configPath))
            {
                lastConfigWriteTime = File.GetLastWriteTimeUtc(configPath);
            }

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
                    .SetMessageHandler<ServerConfigPacket>(OnConfigPacketReceived);
            }
            
        }

        public void CheckConfigChanges(float dt)
        {
            if (!File.Exists(configPath)) return;

            var currentWriteTime = File.GetLastWriteTimeUtc(configPath);

            if (currentWriteTime <= lastConfigWriteTime) return;

            lastConfigWriteTime = currentWriteTime;

            ReloadConfig();
        }

        private void ReloadConfig()
        {
            Config = new LoadOrCreate().SapiConfig(api, ConfigServerName);

            if (Config == null)
            {
                api.Logger.Error($"issue with {ConfigServerName}");
                return;
            }

            WearableLightConfigHelper.Init(Config);

            if (api is ICoreServerAPI sapi)
            {
                foreach (var player in sapi.World.AllOnlinePlayers)
                {
                    if (player is IServerPlayer sp)
                    {
                        SendToPlayer(sp);
                    }
                }
            }

            api.Logger.Debug("[AWearableLight] Config reloaded");
        }

        public void SendToPlayer(IServerPlayer player)
        {
            if (serverChannel == null || Config == null) return;

            var packet = new ServerConfigPacket();

            foreach (var entry in Config.ItemTypes)
            {
                packet.ItemTypes.Add(new ItemTypePacket
                {
                    Code = entry.Key,
                    PowerType = entry.Value.PowerType,
                    OnCraftedFueled = entry.Value.OnCraftedFueled,
                    UseFuel = entry.Value.UseFuel,
                    InLiquid = entry.Value.InLiquid,
                    RainExtinguish = entry.Value.RainExtinguish,
                    Capacity = entry.Value.Capacity,
                    LightHsv = entry.Value.LightHsv,
                    FuelList = entry.Value.FuelList
                });
            }

            serverChannel.SendPacket(packet, player);
        }

        private void OnConfigPacketReceived(ServerConfigPacket packet)
        {
            var capi = api as ICoreClientAPI;

            capi?.Logger.Notification("[WearableLight] Config synced from server");

            var config = new Server
            {
                ItemTypes = new Dictionary<string, FuelProps>()
            };

            foreach (var entry in packet.ItemTypes)
            {
                config.ItemTypes[entry.Code] = new FuelProps
                {
                    PowerType = entry.PowerType,
                    OnCraftedFueled = entry.OnCraftedFueled,
                    UseFuel = entry.UseFuel,
                    InLiquid = entry.InLiquid,
                    RainExtinguish = entry.RainExtinguish,
                    Capacity = entry.Capacity,
                    LightHsv = entry.LightHsv,
                    FuelList = entry.FuelList
                };
            }

            // Init helper
            WearableLightConfigHelper.Init(config);

            // Refresh UI (IMPORTANT)
            capi?.World.Player?.InventoryManager.BroadcastHotbarSlot();
        }
    }
}
  
