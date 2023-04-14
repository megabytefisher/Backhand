using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadSysInfoRequest : DlpArgument
    {
        [BinarySerialize]
        public ushort HostDlpVersionMajor { get; set; }

        [BinarySerialize]
        public ushort HostDlpVersionMinor { get; set; }
    }
}
