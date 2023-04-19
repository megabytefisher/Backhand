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
        public class DatabaseMetadata : DlpArgument
        {
            [BinarySerialize]
            public byte Length { get; set; }

            [BinarySerialize]
            public byte MiscFlags { get; set; }

            [BinarySerialize]
            public DlpDatabaseAttributes Attributes { get; set; }

            [BinarySerialize]
            private FixedSizeBinaryString TypeString { get; } = new(4);

            [BinarySerialize]
            private FixedSizeBinaryString CreatorString { get; } = new(4);

            [BinarySerialize]
            public ushort Version { get; set; }

            [BinarySerialize]
            public uint ModificationNumber { get; set; }

            [BinarySerialize]
            private DlpDateTime CreationDlpDate { get; } = new();

            [BinarySerialize]
            private DlpDateTime ModificationDlpDate { get; } = new();

            [BinarySerialize]
            private DlpDateTime LastBackupDlpDate { get; } = new();

            [BinarySerialize]
            public ushort Index { get; set; }

            [BinarySerialize]
            private NullTerminatedBinaryString NameString { get; } = new();

            public string Type
            {
                get => TypeString.Value;
                set => TypeString.Value = value;
            }

            public string Creator
            {
                get => CreatorString.Value;
                set => CreatorString.Value = value;
            }

            public string Name
            {
                get => NameString.Value;
                set => NameString.Value = value;
            }

            public DateTime CreationDate
            {
                get => CreationDlpDate.AsDateTime;
                set => CreationDlpDate.AsDateTime = value;
            }

            public DateTime ModificationDate
            {
                get => ModificationDlpDate.AsDateTime;
                set => ModificationDlpDate.AsDateTime = value;
            }

            public DateTime LastBackupDate
            {
                get => LastBackupDlpDate.AsDateTime;
                set => LastBackupDlpDate.AsDateTime = value;
            }
        }
    }
}
