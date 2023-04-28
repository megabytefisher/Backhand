using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Data
{
    [GenerateBinarySerialization(MinimumLengthProperty = nameof(Length))]
    public partial class DatabaseMetadata : IBinarySerializable
    {
        [BinarySerialize] public byte Length { get; set; }
        [BinarySerialize] public byte MiscFlags { get; set; }
        [BinarySerialize] public DlpDatabaseAttributes Attributes { get; set; }
        [BinarySerialize] private FixedSizeBinaryString TypeString { get; } = new(4);
        [BinarySerialize] private FixedSizeBinaryString CreatorString { get; } = new(4);
        [BinarySerialize] public ushort Version { get; set; }
        [BinarySerialize] public uint ModificationNumber { get; set; }
        [BinarySerialize] private DlpDateTime CreationDlpDate { get; } = new();
        [BinarySerialize] private DlpDateTime ModificationDlpDate { get; } = new();
        [BinarySerialize] private DlpDateTime LastBackupDlpDate { get; } = new();
        [BinarySerialize] public ushort Index { get; set; }
        [BinarySerialize] private NullTerminatedBinaryString NameString { get; } = new();

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