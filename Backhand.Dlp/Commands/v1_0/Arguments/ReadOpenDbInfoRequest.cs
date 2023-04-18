using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadOpenDbInfoRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }
    }
}
