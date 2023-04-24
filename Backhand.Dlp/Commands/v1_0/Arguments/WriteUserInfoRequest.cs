using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class WriteUserInfoRequest : IBinarySerializable
    {
        [BinarySerialize]
        public uint UserId { get; set; }

        [BinarySerialize]
        public uint ViewerId { get; set; }

        [BinarySerialize]
        public uint LastSyncPcId { get; set; }

        [BinarySerialize]
        private DlpDateTime LastSuccessfulSyncDlpDate { get; set; } = new();

        [BinarySerialize]
        private byte Padding { get; set; } = 0xff;

        [BinarySerialize]
        public byte UsernameByteLength
        {
            get => (byte)UsernameString.Value.Length;
            set => UsernameString.Value = UsernameString.Value[..value];
        }

        [BinarySerialize]
        private NullTerminatedBinaryString UsernameString { get; set; } = new();

        public string Username
        {
            get => UsernameString.Value;
            set => UsernameString.Value = value;
        }

        public DateTime LastSyncDate
        {
            get => LastSuccessfulSyncDlpDate.AsDateTime;
            set => LastSuccessfulSyncDlpDate.AsDateTime = value;
        }
    }
}