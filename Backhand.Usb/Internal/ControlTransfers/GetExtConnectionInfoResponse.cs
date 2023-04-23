using Backhand.Common.BinarySerialization;

namespace Backhand.Usb.Internal.ControlTransfers
{
    [GenerateBinarySerialization]
    internal partial class GetExtConnectionInfoResponse : IBinarySerializable
    {
        [BinarySerialize]
        public byte PortCount
        {
            get => (byte)Ports.Length;
            set => Ports = Enumerable.Range(1, value).Select(_ => new ExtConnectionPortInfo()).ToArray();
        }

        [BinarySerialize]
        public bool HasDifferentEndpoints { get; set; }

        [BinarySerialize]
        public byte[] Padding {get; set; } = new byte[2];

        [BinarySerialize]
        public ExtConnectionPortInfo[] Ports { get; private set; } = Array.Empty<ExtConnectionPortInfo>();
    }
}