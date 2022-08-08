using Backhand.Pdb.Utility;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.Internal
{
    public class FileDatabaseHeader
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
            32 +                                        // Name
            sizeof(DatabaseAttributes) +                // Attributes
            sizeof(ushort) +                            // Version
            BufferUtilities.DatabaseTimestampLength +   // CreationDate
            BufferUtilities.DatabaseTimestampLength +   // ModificationDate
            BufferUtilities.DatabaseTimestampLength +   // LastBackupDate
            sizeof(uint) +                              // ModificationNumber
            sizeof(uint) +                              // AppInfoId
            sizeof(uint) +                              // SortInfoId
            4 +                                         // Type
            4 +                                         // Creator
            sizeof(uint);                               // UniqueIdSeed

        public void Serialize(Span<byte> buffer)
        {
            int offset = 0;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, 32), Name);
            offset += 32;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)Attributes);
            offset += 2;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), Version);
            offset += 2;

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), CreationDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), ModificationDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BufferUtilities.WriteDatabaseTimestamp(buffer.Slice(offset, BufferUtilities.DatabaseTimestampLength), LastBackupDate);
            offset += BufferUtilities.DatabaseTimestampLength;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), ModificationNumber);
            offset += 4;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), AppInfoId);
            offset += 4;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), SortInfoId);
            offset += 4;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, 4), Type);
            offset += 4;

            BufferUtilities.WriteFixedLengthString(buffer.Slice(offset, 4), Creator);
            offset += 4;

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), UniqueIdSeed);
            offset += 4;
        }

        public SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Name = BufferUtilities.ReadFixedLengthString(ref bufferReader, 32);
            Attributes = (DatabaseAttributes)bufferReader.ReadUInt16BigEndian();
            Version = bufferReader.ReadUInt16BigEndian();
            CreationDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            ModificationDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            LastBackupDate = BufferUtilities.ReadDatabaseTimestamp(ref bufferReader);
            ModificationNumber = bufferReader.ReadUInt32BigEndian();
            AppInfoId = bufferReader.ReadUInt32BigEndian();
            SortInfoId = bufferReader.ReadUInt32BigEndian();
            Type = BufferUtilities.ReadFixedLengthString(ref bufferReader, 4);
            Creator = BufferUtilities.ReadFixedLengthString(ref bufferReader, 4);
            UniqueIdSeed = bufferReader.ReadUInt32BigEndian();
        }
    }
}
