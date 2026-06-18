
using Vintagestory.API.Common;

namespace AWearableLight.Interfaces
{
    public interface IPowerSource
    {
        bool HasPower(ItemStack sourceStack);

        bool IsFull(ItemStack sourceStack);

        double GetPower(ItemStack sourceStack);

        float GetStackFuel(ItemStack sourceStack, ItemStack sinkStack);

        void SetPower(ItemStack sourceStack, double amount);

        void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount);

        void ConsumePower(ItemSlot sourceslot, IPlayer player, double amount);
    }

}
