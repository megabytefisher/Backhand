using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadSysInfoDlpResponse : IBinarySerializable
    {
        [BinarySerialize]
        public ushort ClientDlpVersionMajor { get; set; }

        [BinarySerialize]
        public ushort ClientDlpVersionMinor { get; set; }

        [BinarySerialize]
        public ushort MinimumDlpVersionMajor { get; set; }

        [BinarySerialize]
        public ushort MinimumDlpVersionMinor { get; set; }

        [BinarySerialize]
        public uint MaxRecordSize { get; set; }
    }
}
