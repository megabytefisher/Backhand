using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;
using System.Linq;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadDbListResponse : DlpArgument
    {
        [BinarySerialize]
        public ushort LastIndex { get; set; }

        [BinarySerialize]
        public byte Flags { get; set; }

        [BinarySerialize]
        public byte ResultCount
        {
            get => (byte)Results.Length;
            set => Results = Enumerable.Range(1, value).Select(_ => new DatabaseMetadata()).ToArray();
        }

        [BinarySerialize]
        public DatabaseMetadata[] Results { get; private set; } = Array.Empty<DatabaseMetadata>();

        [BinarySerializable(MinimumLengthProperty = nameof(Length))]
        public class DatabaseMetadata
        {
            [BinarySerialize]
            public byte Length { get; set; }

            [BinarySerialize]
            public byte MiscFlags { get; set; }

            [BinarySerialize]
            public DlpDatabaseAttributes Attributes { get; set; }

            [BinarySerialize(Length = 4)]
            public string Type { get; set; } = string.Empty;

            [BinarySerialize(Length = 4)]
            public string Creator { get; set; } = string.Empty;

            [BinarySerialize]
            public ushort Version { get; set; }

            [BinarySerialize]
            public uint ModificationNumber { get; set; }

            [BinarySerialize]
            public byte[] CreationDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

            [BinarySerialize]
            public byte[] ModificationDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

            [BinarySerialize]
            public byte[] LastBackupDateBytes { get; set; } = new byte[DlpPrimitives.DlpDateTimeSize];

            [BinarySerialize]
            public ushort Index { get; set; }

            [BinarySerialize(NullTerminated = true)]
            public string Name { get; set; } = string.Empty;

            public DateTime CreationDate
            {
                get => DlpPrimitives.ReadDlpDateTime(CreationDateBytes);
                set => DlpPrimitives.WriteDlpDateTime(CreationDateBytes, value);
            }

            public DateTime ModificationDate
            {
                get => DlpPrimitives.ReadDlpDateTime(ModificationDateBytes);
                set => DlpPrimitives.WriteDlpDateTime(ModificationDateBytes, value);
            }

            public DateTime LastBackupDate
            {
                get => DlpPrimitives.ReadDlpDateTime(LastBackupDateBytes);
                set => DlpPrimitives.WriteDlpDateTime(LastBackupDateBytes, value);
            }
        }
    }
}
