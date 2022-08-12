using Backhand.Pdb.Internal;
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
        public string Name { get; set; } = "";
        public DatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public uint ModificationNumber { get; set; }
        public string Type { get; set; } = "";
        public string Creator { get; set; } = "";
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
                readOffset += await stream.ReadAsync(buffer.Slice(readOffset));
            } while (readOffset < buffer.Length);
        }

        protected async Task SerializeHeaderAsync(Stream stream, uint appInfoId, uint sortInfoId)
        {
            FileDatabaseHeader fileHeader = new FileDatabaseHeader
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

            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            fileHeader.Serialize(buffer);

            await stream.WriteAsync(buffer);
        }

        protected async Task<(uint appInfoId, uint sortInfoId)> DeserializeHeaderAsync(Stream stream)
        {
            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileDatabaseHeader fileHeader = new FileDatabaseHeader();
            fileHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            Name = fileHeader.Name;
            Attributes = fileHeader.Attributes;
            Version = fileHeader.Version;
            CreationDate = fileHeader.CreationDate;
            ModificationDate = fileHeader.ModificationDate;
            LastBackupDate = fileHeader.LastBackupDate;
            ModificationNumber = fileHeader.ModificationNumber;
            Type = fileHeader.Type;
            Creator = fileHeader.Creator;
            UniqueIdSeed = fileHeader.UniqueIdSeed;

            return (fileHeader.AppInfoId, fileHeader.SortInfoId);
        }

        protected async Task SerializeEntryMetadataListHeaderAsync(Stream stream, ushort entryCount)
        {
            FileEntryMetadataListHeader fileEntryMetadataListHeader = new FileEntryMetadataListHeader
            {
                NextListId = 0,
                Length = entryCount
            };

            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            fileEntryMetadataListHeader.Serialize(buffer);

            await stream.WriteAsync(buffer);
        }

        protected async Task<ushort> DeserializeEntryMetadataListHeaderAsync(Stream stream)
        {
            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileEntryMetadataListHeader fileEntryMetadataListHeader = new FileEntryMetadataListHeader();
            fileEntryMetadataListHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            if (fileEntryMetadataListHeader.NextListId != 0)
                throw new Exception("Database files with more than one entry list are not supported");

            return fileEntryMetadataListHeader.Length;
        }
    }
}
