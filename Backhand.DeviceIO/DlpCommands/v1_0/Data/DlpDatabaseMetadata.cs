using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class DlpDatabaseMetadata : DlpArgument
    {
        public byte MiscFlags { get; private set; }
        public DlpDatabaseAttributes Attributes { get; private set; }
        public string Type { get; private set; } = "";
        public string Creator { get; private set; } = "";
        public ushort Version { get; private set; }
        public uint ModificationNumber { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime ModificationDate { get; private set; }
        public DateTime LastBackupDate { get; private set; }
        public ushort Index { get; private set; }
        public string Name { get; private set; } = "";

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            byte length = bufferReader.Read();

            ReadOnlySequence<byte> metadataBuffer = bufferReader.Sequence.Slice(bufferReader.Position, length - 1);
            SequenceReader<byte> metadataReader = new(metadataBuffer);

            MiscFlags = metadataReader.Read();
            Attributes = (DlpDatabaseAttributes)metadataReader.ReadUInt16BigEndian();
            Type = ReadFixedLengthString(ref metadataReader, sizeof(byte) * 4);
            Creator = ReadFixedLengthString(ref metadataReader, sizeof(byte) * 4);
            Version = metadataReader.ReadUInt16BigEndian();
            ModificationNumber = metadataReader.ReadUInt32BigEndian();
            CreationDate = ReadDlpDateTime(ref metadataReader);
            ModificationDate = ReadDlpDateTime(ref metadataReader);
            LastBackupDate = ReadDlpDateTime(ref metadataReader);
            Index = metadataReader.ReadUInt16BigEndian();
            Name = ReadNullTerminatedString(ref metadataReader);

            bufferReader.Advance(length - 1);
        }
    }
}
