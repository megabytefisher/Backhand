using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerialized(Endian = Endian.Big)]
    public class ReadUserInfoResponse : DlpArgument
    {
        [BinarySerialized]
        public uint UserId { get; set; }

        [BinarySerialized]
        public uint ViewerId { get; set; }

        [BinarySerialized]
        public uint LastSyncPcId { get; set; }

        [BinarySerialized]
        public byte[] LastSuccessfulSyncDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

        [BinarySerialized]
        public byte[] LastSyncDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

        [BinarySerialized]
        public byte UsernameByteLength { get; set; }

        [BinarySerialized]
        public byte PasswordByteLength
        {
            get => Convert.ToByte(Password.Length);
            set => Password = new byte[value];
        }

        [BinarySerialized(ConditionName = nameof(ShouldSerializeUsername), LengthName = nameof(UsernameByteLength), NullTerminated = true)]
        public string Username { get; set; } = string.Empty;

        [BinarySerialized(ConditionName = nameof(ShouldSerializePassword))]
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
