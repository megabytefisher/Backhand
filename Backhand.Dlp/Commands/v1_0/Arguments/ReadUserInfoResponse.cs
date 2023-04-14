using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

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
        public byte[] LastSuccessfulSyncDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

        [BinarySerialize]
        public byte[] LastSyncDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

        [BinarySerialize]
        public byte UsernameByteLength { get; set; }

        [BinarySerialize]
        public byte PasswordByteLength
        {
            get => Convert.ToByte(Password.Length);
            set => Password = new byte[value];
        }

        [BinarySerialize(ConditonProperty = nameof(ShouldSerializeUsername), LengthProperty = nameof(UsernameByteLength), NullTerminated = true)]
        public string Username { get; set; } = string.Empty;

        [BinarySerialize(ConditonProperty = nameof(ShouldSerializePassword))]
        public byte[] Password { get; private set; } = Array.Empty<byte>();

        public bool ShouldSerializeUsername => UsernameByteLength > 0;
        public bool ShouldSerializePassword => PasswordByteLength > 0;

        public DateTime LastSuccessfulSyncDate
        {
            get => DlpPrimitives.ReadDlpDateTime(LastSuccessfulSyncDateBytes);
            set => DlpPrimitives.WriteDlpDateTime(LastSuccessfulSyncDateBytes, value);
        }

        public DateTime LastSyncDate
        {
            get => DlpPrimitives.ReadDlpDateTime(LastSyncDateBytes);
            set => DlpPrimitives.WriteDlpDateTime(LastSyncDateBytes, value);
        }
    }
}
