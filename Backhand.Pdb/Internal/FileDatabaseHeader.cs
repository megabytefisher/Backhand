using Backhand.Pdb.Utility;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Backhand.Pdb.Internal
{
    internal class FileDatabaseHeader
    {
        public string Name { get; set; } = string.Empty;
        public DatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public uint ModificationNumber { get; set; }
        public uint AppInfoId { get; set; }
        public uint SortInfoId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public uint UniqueIdSeed { get; set; }

        public const uint SerializedLength =
            (sizeof(byte) * 32) +                       // Name
            sizeof(ushort) +                            // Attributes
            sizeof(ushort) +                            // Version
            BufferUtilities.DatabaseTimestampLength +   // CreationDate
            BufferUtilities.DatabaseTimestampLength +   // ModificationDate
            BufferUtilities.DatabaseTimestampLength +   // LastBackupDate
            sizeof(uint) +                              // ModificationNumber
            sizeof(uint) +                              // AppInfoId
            sizeof(uint) +                              // SortInfoId
            (sizeof(byte) * 4) +                        // Type
            (sizeof(byte) * 4) +                        // Creator
            sizeof(uint);                               // UniqueIdSeed

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 32), Name);
            offset += sizeof(byte) * 32;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), (ushort)Attributes);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Version);
            offset += sizeof(ushort);

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), CreationDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), ModificationDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), LastBackupDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), ModificationNumber);
            offset += sizeof(uint);

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), AppInfoId);
            offset += sizeof(uint);

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), SortInfoId);
            offset += sizeof(uint);

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Type);
            offset += sizeof(byte) * 4;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, sizeof(byte) * 4), Creator);
            offset += sizeof(byte) * 4;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), UniqueIdSeed);
            offset += sizeof(uint);
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Name = BufferUtilities.ReadFixedLengthString(ref bufferReader, sizeof(byte) * 32);
            Attributes = (DatabaseAttributes)bufferReader.ReadUInt16BigEndian();
            Version = bufferReader.ReadUInt16BigEndian();
            CreationDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            ModificationDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            LastBackupDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            ModificationNumber = bufferReader.ReadUInt32BigEndian();
            AppInfoId = bufferReader.ReadUInt32BigEndian();
            SortInfoId = bufferReader.ReadUInt32BigEndian();
            Type = BufferUtilities.ReadFixedLengthString(ref bufferReader, sizeof(byte) * 4);
            Creator = BufferUtilities.ReadFixedLengthString(ref bufferReader, sizeof(byte) * 4);
            UniqueIdSeed = bufferReader.ReadUInt32BigEndian();
        }
    }
}
