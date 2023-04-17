using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class WriteUserInfoRequest : DlpArgument
    {
        [BinarySerialize]
        public uint UserId { get; set; }

        [BinarySerialize]
        public uint ViewerId { get; set; }

        [BinarySerialize]
        public uint LastSyncPcId { get; set; }

        [BinarySerialize]
        public byte[] LastSuccessfulSyncDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

        [BinarySerialize]
        public byte Padding { get; set; }

        [BinarySerialize]
        public byte UsernameByteLength { get; set; }

        [BinarySerialize]
        public string Username { get; set; } = string.Empty;
    }
}