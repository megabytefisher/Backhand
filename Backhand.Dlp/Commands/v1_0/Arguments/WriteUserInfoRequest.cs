using System;
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
        private DlpDateTime LastSuccessfulSyncDlpDate { get; set; } = new();

        [BinarySerialize]
        public byte Padding { get; set; }

        [BinarySerialize]
        public byte UsernameByteLength { get; set; }

        [BinarySerialize]
        private NullTerminatedBinaryString UsernameString { get; set; } = new();

        public string Username
        {
            get => UsernameString.Value;
            set => UsernameString.Value = value;
        }

        public DateTime LastSuccessfulSyncDate
        {
            get => LastSuccessfulSyncDlpDate.AsDateTime;
            set => LastSuccessfulSyncDlpDate.AsDateTime = value;
        }
    }
}