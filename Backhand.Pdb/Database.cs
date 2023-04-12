using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using Backhand.Pdb.FileSerialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public abstract class Database
    {
        public string Name { get; set; } = string.Empty;
        public DatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public uint ModificationNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public uint UniqueIdSeed { get; set; }

        public byte[]? AppInfo { get; set; }
        public byte[]? SortInfo { get; set; }

        protected const int HeaderPaddingLength = 2;

        public abstract Task SerializeAsync(Stream stream);
        public abstract Task DeserializeAsync(Stream stream);

        protected static async Task FillBuffer(Stream stream, Memory<byte> buffer)
        {
            int readOffset = 0;
            do
            {
                readOffset += await stream.ReadAsync(buffer.Slice(readOffset)).ConfigureAwait(false);
            } while (readOffset < buffer.Length);
        }

        protected async Task WriteHeaderAsync(Stream stream, uint appInfoId, uint sortInfoId)
        {
            DatabaseFileHeader header = new DatabaseFileHeader
            {
                Name = Name,
                Attributes = Attributes,
                Version = Version,
                CreationDate = CreationDate,
                ModificationDate = ModificationDate,
                LastBackupDate = LastBackupDate,
                ModificationNumber = ModificationNumber,
                AppInfoId = appInfoId,
                SortInfoId = sortInfoId,
                Type = Type,
                Creator = Creator,
                UniqueIdSeed = UniqueIdSeed,
            };

            byte[] buffer = new byte[BinarySerializer<DatabaseFileHeader>.GetSize(header)];
            WriteHeader(buffer, header);
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }

        protected async Task<(uint appInfoId, uint sortInfoId)> ReadHeaderAsync(Stream stream)
        {
            DatabaseFileHeader header = new();
            byte[] buffer = new byte[BinarySerializer<DatabaseFileHeader>.GetSize(header)];
            await FillBuffer(stream, buffer).ConfigureAwait(false);
            ReadHeader(new ReadOnlySequence<byte>(buffer), header);

            Name = header.Name;
            Attributes = header.Attributes;
            Version = header.Version;
            CreationDate = header.CreationDate;
            ModificationDate = header.ModificationDate;
            LastBackupDate = header.LastBackupDate;
            ModificationNumber = header.ModificationNumber;
            Type = header.Type;
            Creator = header.Creator;
            UniqueIdSeed = header.UniqueIdSeed;

            return (header.AppInfoId, header.SortInfoId);
        }

        protected async Task WriteEntryListHeaderAsync(Stream stream, ushort entryCount)
        {
            DatabaseFileEntryListHeader header = new DatabaseFileEntryListHeader
            {
                NextListId = 0,
                Length = entryCount
            };

            byte[] buffer = new byte[BinarySerializer<DatabaseFileEntryListHeader>.GetSize(header)];
            WriteEntryListHeader(buffer, header);
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }

        protected async Task<ushort> ReadEntryListHeaderAsync(Stream stream)
        {
            DatabaseFileEntryListHeader header = new();
            byte[] buffer = new byte[BinarySerializer<DatabaseFileEntryListHeader>.GetSize(header)];
            await FillBuffer(stream, buffer).ConfigureAwait(false);
            ReadEntryListHeader(new ReadOnlySequence<byte>(buffer), header);

            return header.Length;
        }

        private static void WriteHeader(Span<byte> buffer, DatabaseFileHeader header)
        {
            SpanWriter<byte> bufferWriter = new SpanWriter<byte>(buffer);
            BinarySerializer<DatabaseFileHeader>.Serialize(header, ref bufferWriter);
        }

        private static void ReadHeader(ReadOnlySequence<byte> buffer, DatabaseFileHeader header)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            BinarySerializer<DatabaseFileHeader>.Deserialize(ref bufferReader, header);
        }

        private static void WriteEntryListHeader(Span<byte> buffer, DatabaseFileEntryListHeader header)
        {
            SpanWriter<byte> bufferWriter = new SpanWriter<byte>(buffer);
            BinarySerializer<DatabaseFileEntryListHeader>.Serialize(header, ref bufferWriter);
        }

        private static void ReadEntryListHeader(ReadOnlySequence<byte> buffer, DatabaseFileEntryListHeader header)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            BinarySerializer<DatabaseFileEntryListHeader>.Deserialize(ref bufferReader, header);
        }
    }
}
