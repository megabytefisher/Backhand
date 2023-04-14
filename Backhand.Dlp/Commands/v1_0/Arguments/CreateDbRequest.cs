using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class CreateDbRequest : DlpArgument
    {
        [BinarySerialize(Length = 4)]
        public string Creator { get; set; } = string.Empty;

        [BinarySerialize(Length = 4)]
        public string Type { get; set; } = string.Empty;

        [BinarySerialize]
        public byte CardId { get; set; }

        [BinarySerialize]
        public byte Padding { get; set; } = 0;

        [BinarySerialize]
        public DlpDatabaseAttributes Attributes { get; set; }

        [BinarySerialize]
        public ushort Version { get; set; }

        [BinarySerialize(NullTerminated = true)]
        public string Name { get; set; } = string.Empty;
    }
}
