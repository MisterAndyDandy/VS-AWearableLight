using AWearableLight.Config;
using AWearableLight.Util;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AWearableLight
{
    #nullable enable

    public class AWearableLightCore : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Server)
            {
                ConfigConstants.ServerConfig = new LoadOrCreate().SapiConfig(api, ConfigConstants.ConfigServerName);
                WearableLightConfigHelper.Init(ConfigConstants.ServerConfig);

                if (WearableLightConfigHelper.oldVersion >= ConfigConstants.ServerConfig.Version)
                {
                    WearableLightConfigHelper.MigrateConfig(api);
                }
            }

            api.RegisterCollectibleBehaviorClass("awearablelight:AttachmentableLight", typeof(CollectibleBehaviorAttachmentableLight));
            api.RegisterCollectibleBehaviorClass("awearablelight:Fuel", typeof(CollectibleBehaviorFuel));
            api.RegisterCollectibleBehaviorClass("awearablelight:WindUp", typeof(CollectibleBehaviorWindup));

        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            ConfigConstants.ClientConfig = new LoadOrCreate().CapiConfig(api, ConfigConstants.ConfigClientName);

            //_ = new FuelHudRenderer(api);
        }
    }
}
