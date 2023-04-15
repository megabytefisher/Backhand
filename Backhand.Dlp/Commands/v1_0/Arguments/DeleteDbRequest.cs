using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class DeleteDbRequest : DlpArgument
    {
        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize(NullTerminated = true)]
        public string Name { get; set; } = string.Empty;
    }
}
