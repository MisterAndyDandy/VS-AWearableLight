
using Vintagestory.API.Common;

namespace AWearableLight.Interfaces
{
    public interface IPowerSource
    {
        bool HasPower(ItemStack sourceStack);

        bool IsFull(ItemStack sourceStack);

        double GetPower(ItemStack sourceStack);

        void SetPower(ItemStack sourceStack, double amount);

        void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount);

        void ConsumePower(ItemStack sourceStack, double amount);
    }

}
