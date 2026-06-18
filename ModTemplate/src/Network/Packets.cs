using ProtoBuf;

namespace AWearableLight.Network
{
    [ProtoContract]
    public sealed class FuelSyncPacket
    {
        [ProtoMember(1)]
        public double FuelHours;

        [ProtoMember(2)]
        public string InvClass;

        [ProtoMember(3)]
        public int IdSlot;
    }

    [ProtoContract]
    public sealed class AWearableLightPacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }
    }
}
