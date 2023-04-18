using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadStorageInfoRequest : DlpArgument
    {
        [BinarySerialize]
        public byte CardNo { get; set; }

        [BinarySerialize]
        private byte Padding { get; set; } = 0;
    }
}
