using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace AWearableLight.Render
{

    public class FuelHudRenderer : IRenderer
    {
        public double RenderOrder => 1;
        public int RenderRange => 0;

        public ICoreClientAPI Capi { get; set; }

        public FuelHudRenderer(ICoreClientAPI api)
        {
            Capi = api;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
           var inv = Capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

            if (inv != null)
            {
            }
        }

        public void Dispose()
        {
           
        }
    }
}
