using Backhand.Common.BinarySerialization;

namespace Backhand.Protocols.Cmp.Internal
{
    [GenerateBinarySerialization]
    internal partial class CmpInitPacket : IBinarySerializable
    {
        [BinarySerialize]
        public CmpConnection.CmpPacketType Type { get; set; } = CmpConnection.CmpPacketType.Init;

        [BinarySerialize]
        public CmpConnection.CmpInitPacketFlags Flags { get; set; }

        [BinarySerialize]
        public byte MajorVersion { get; set; }

        [BinarySerialize]
        public byte MinorVersion { get; set; }

        [BinarySerialize]
        private byte Padding1 { get; set; }

        [BinarySerialize]
        private byte Padding2 { get; set; }

        [BinarySerialize]
        public uint NewBaudRate { get; set; }
    }
}