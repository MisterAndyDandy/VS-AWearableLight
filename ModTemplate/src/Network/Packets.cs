using ProtoBuf;

namespace AWearableLight.Network
{

    [ProtoContract]
    public sealed class AWearableLightPacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }
    }

}
