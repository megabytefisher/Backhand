using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Usb.Internal.ControlTransfers
{
    [GenerateBinarySerialization]
    internal partial class ExtConnectionPortInfo : IBinarySerializable
    {
        [BinarySerialize]
        public FixedSizeBinaryString Type { get; set; } = new(4);

        [BinarySerialize]
        public byte PortNumber { get; set; }

        [BinarySerialize]
        public byte Endpoints { get; set; }

        [BinarySerialize]
        public byte[] Padding { get; set; } = new byte[2];

        public byte InEndpoint => (byte)((Endpoints & ExtConnectionInEndpointBitmask) >> ExtConnectionInEndpointShift);
        public byte OutEndpoint => (byte)((Endpoints & ExtConnectionOutEndpointBitmask) >> ExtConnectionOutEndpointShift);

        private const int ExtConnectionInEndpointBitmask = 0b11110000;
        private const int ExtConnectionInEndpointShift = 4;
        private const int ExtConnectionOutEndpointBitmask = 0b00001111;
        private const int ExtConnectionOutEndpointShift = 0;
    }
}