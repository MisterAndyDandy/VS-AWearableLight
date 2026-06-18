using AWearableLight.Interfaces;
using AWearableLight.Network;
using AWearableLight.Util;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AWearableLight.System
{
    #nullable enable
    public class OnGameTick: ModSystem
    {
        public ICoreServerAPI? Sapi;

        public long OnLongRefreshTick { get; set; }

        public long OnLongServerFuelTick { get; set; }

        public long OnLongServerTick { get; set; }

        public double LastServerTotalHours { get; set; } = 0;

        public ConfigSyncSystem? ConfigSync { get; set; }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            ConfigSync = new ConfigSyncSystem(
                api,
                api.Side == EnumAppSide.Server ? ConfigConstants.ServerConfig : null
            );

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            Sapi = api;

            if(ConfigSync != null)
            {
                api.Event.PlayerNowPlaying += (player) =>
                {
                    ConfigSync.SendToPlayer(player);
                };

                OnLongRefreshTick = api.Event.RegisterGameTickListener(ConfigSync.CheckConfigChanges, 100000);
            }

            api.Event.DidUseBlock += Event_DidUseBlock;

            OnLongServerFuelTick = api.Event.RegisterGameTickListener(OnServerFuelTick, 10000);
            OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 100);
        }

        public void Event_DidUseBlock(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            if (Sapi == null) return;

            var mann = Sapi.World.BlockAccessor.GetBlockEntity(blockSel?.Position)?.GetBehavior<BEBehaviorMannequin>();

            if(mann?.Inventory != null)
            {
                ItemSlot? slot = null;

                mann.Inventory.ToList().ForEach(m =>
                {
                    if (m.Empty) return;

                    if(m.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorAttachmentableLight>(true) != null)
                    {
                        slot = m;
                        
                    }
                });

               var id = mann.Inventory.GetSlotId(slot);

                if (id == -1) return;

               var light = mann.Inventory[id]?.Itemstack?.Collectible.LightHsv;

                if (light?[2] > 0)
                {
                    mann.Blockentity.Block.LightHsv = (Vintagestory.API.MathTools.ThreeBytes)light;
                    mann.Blockentity.MarkDirty();
                }
                else
                {
                    mann.Block.LightHsv = new byte[3];
                }
        }
        }

        public override void Dispose()
        {
            base.Dispose();

            Sapi?.Event.UnregisterGameTickListener(OnLongServerFuelTick);
            Sapi?.Event.UnregisterGameTickListener(OnLongRefreshTick);
            Sapi?.Event.UnregisterGameTickListener(OnLongServerTick);

            ConfigSync = null;
            ConfigConstants.ServerConfig = null;
            ConfigConstants.ClientConfig = null;
        }

        public void OnServerTick(float obj)
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

                    if (!WearableLightConfigHelper.NeedFuel(
                        slot.Itemstack.Collectible.Code))
                    {
                        continue;
                    }

                    var dresstype = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorWearable>(true)?.GetDressType(slot);

                    if (item.InLiquid(player.Entity, slot.Itemstack, dresstype) || item.GetWeather(player.Entity, slot.Itemstack))
                    {
                        Sapi.ModLoader.GetModSystem<NetworkHandler>().ToggleWearableItem(player, new AWearableLightPacket() { ItemSlot = invGear.GetSlotId(slot) });
                    }
                }
            }
        }

        public void OnServerFuelTick(float dt)
        {
            if (Sapi == null) return;

            double totalHours = Sapi.World.Calendar.TotalHours;
            double hoursPassed = totalHours - LastServerTotalHours;

            hoursPassed = Math.Min(hoursPassed, 0.5);

            if (hoursPassed <= 0)
            {
                LastServerTotalHours = totalHours;
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
                            player,
                            hoursPassed
                        );

                    }
                }
            }

            LastServerTotalHours = totalHours;
        }
    }
}
