using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;
using System.Text;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadUserInfoResponse : DlpArgument
    {
        [BinarySerialize]
        public uint UserId { get; set; }

        [BinarySerialize]
        public uint ViewerId { get; set; }

        [BinarySerialize]
        public uint LastSyncPcId { get; set; }

        [BinarySerialize]
        private DlpDateTime LastSuccessfulSyncDlpDate { get; } = new();

        [BinarySerialize]
        private DlpDateTime LastSyncDlpDate { get; } = new();

        [BinarySerialize]
        private byte UsernameSize
        {
            get => Convert.ToByte(UsernameString.Size);
            set => UsernameString.Size = value;
        }

        [BinarySerialize]
        public byte PasswordByteLength
        {
            get => Convert.ToByte(Password.Length);
            set => Password = new byte[value];
        }

        [BinarySerialize]
        private FixedSizeBinaryString UsernameString { get; set; } = new()
        {
            NullTerminated = true
        };

        [BinarySerialize]
        public byte[] Password { get; private set; } = Array.Empty<byte>();

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

        public DateTime LastSyncDate
        {
            get => LastSyncDlpDate.AsDateTime;
            set => LastSyncDlpDate.AsDateTime = value;
        }
    }
}
