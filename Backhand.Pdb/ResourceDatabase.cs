using Backhand.Pdb.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class ResourceDatabase
    {
        public DatabaseHeader Header { get; set; } = new DatabaseHeader();
        public byte[]? AppInfo { get; set; }
        public byte[]? SortInfo { get; set; }
        public List<DatabaseResource> Resources { get; set; } = new List<DatabaseResource>();

        public async Task Serialize(Stream stream)
        {
            int appInfoOffset =
                FileDatabaseHeader.SerializedLength +
                FileEntryMetadataListHeader.SerializedLength +
                (FileResourceMetadata.SerializedLength * Resources.Count) +
                2;
            int sortInfoOffset = appInfoOffset + (AppInfo?.Length ?? 0);
            int resourceBlockOffset = sortInfoOffset + (SortInfo?.Length ?? 0);

            FileDatabaseHeader fileHeader = new FileDatabaseHeader();
            fileHeader.Name = Header.Name;
            fileHeader.Attributes = Header.Attributes;
            fileHeader.Version = Header.Version;
            fileHeader.CreationDate = Header.CreationDate;
            fileHeader.ModificationDate = Header.ModificationDate;
            fileHeader.LastBackupDate = Header.LastBackupDate;
            fileHeader.ModificationNumber = Header.ModificationNumber;
            fileHeader.AppInfoId = (uint)(AppInfo != null && AppInfo.Length > 0 ? appInfoOffset : 0);
            fileHeader.SortInfoId = (uint)(SortInfo != null && SortInfo.Length > 0 ? sortInfoOffset : 0);
            fileHeader.Type = Header.Type;
            fileHeader.Creator = Header.Creator;
            fileHeader.UniqueIdSeed = Header.UniqueIdSeed;

            await SerializeHeader(stream, fileHeader);

            FileEntryMetadataListHeader metadataListHeader = new FileEntryMetadataListHeader();
            metadataListHeader.NextListId = 0;
            metadataListHeader.Length = Convert.ToUInt16(Resources.Count);

            await SerializeEntryMetadataListHeader(stream, metadataListHeader);

            int blockOffset = 0;
            foreach (DatabaseResource resource in Resources)
            {
                FileResourceMetadata metadata = new FileResourceMetadata();
                metadata.Type = resource.Type;
                metadata.ResourceId = resource.ResourceId;
                metadata.LocalChunkId = (uint)(resourceBlockOffset + blockOffset);

                await SerializeResourceMetadata(stream, metadata);

                blockOffset += resource.Data.Length;
            }

            // Write padding
            await stream.WriteAsync(new byte[] { 0x00, 0x00 });

            // Write AppInfo
            if (AppInfo != null && AppInfo.Length > 0)
            {
                await stream.WriteAsync(AppInfo);
            }

            // Write SortInfo
            if (SortInfo != null && SortInfo.Length > 0)
            {
                await stream.WriteAsync(SortInfo);
            }

            // Write resources...
            foreach (DatabaseResource resource in Resources)
            {
                await stream.WriteAsync(resource.Data);
            }
        }

        public async Task Deserialize(Stream stream)
        {
            FileDatabaseHeader fileHeader = await DeserializeHeader(stream);

            Header.Name = fileHeader.Name;
            Header.Attributes = fileHeader.Attributes;
            Header.Version = fileHeader.Version;
            Header.CreationDate = fileHeader.CreationDate;
            Header.ModificationDate = fileHeader.ModificationDate;
            Header.LastBackupDate = fileHeader.LastBackupDate;
            Header.ModificationNumber = fileHeader.ModificationNumber;
            Header.Type = fileHeader.Type;
            Header.Creator = fileHeader.Creator;
            Header.UniqueIdSeed = fileHeader.UniqueIdSeed;

            FileEntryMetadataListHeader metadataListHeader = await DeserializeEntryMetadataListHeader(stream);

            if (metadataListHeader.NextListId != 0)
                throw new Exception("PRC files with multiple resource lists aren't supported");

            // Read each metadata entry
            List<FileResourceMetadata> metadataList = new List<FileResourceMetadata>();
            for (ushort i = 0; i < metadataListHeader.Length; i++)
            {
                metadataList.Add(await DeserializeResourceMetadata(stream));
            }

            // Read AppInfo block
            if (fileHeader.AppInfoId != 0)
            {
                uint appInfoLength =
                    fileHeader.SortInfoId != 0 ? fileHeader.SortInfoId - fileHeader.AppInfoId :
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - fileHeader.AppInfoId :
                    (uint)stream.Length - fileHeader.AppInfoId;

                stream.Seek(fileHeader.AppInfoId, SeekOrigin.Begin);
                AppInfo = new byte[appInfoLength];
                await FillBuffer(stream, AppInfo);
            }

            // Read SortInfo block
            if (fileHeader.SortInfoId != 0)
            {
                uint sortInfoLength =
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - fileHeader.SortInfoId :
                    (uint)stream.Length - fileHeader.SortInfoId;

                stream.Seek(fileHeader.SortInfoId, SeekOrigin.Begin);
                SortInfo = new byte[sortInfoLength];
                await FillBuffer(stream, SortInfo);
            }

            // Read entries
            Resources.Clear();
            foreach (FileResourceMetadata metadata in metadataList)
            {
                FileResourceMetadata? nextMetadata = metadataList
                    .Where(md => md.LocalChunkId > metadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint resourceLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - metadata.LocalChunkId :
                    (uint)stream.Length - metadata.LocalChunkId;

                stream.Seek(metadata.LocalChunkId, SeekOrigin.Begin);
                byte[] resourceBuffer = new byte[resourceLength];
                await FillBuffer(stream, resourceBuffer);

                DatabaseResource resource = new DatabaseResource();
                resource.Type = metadata.Type;
                resource.ResourceId = metadata.ResourceId;
                resource.Data = resourceBuffer;
                Resources.Add(resource);
            }
        }

        private static async Task FillBuffer(Stream stream, Memory<byte> buffer)
        {
            int readOffset = 0;
            do
            {
                readOffset += await stream.ReadAsync(buffer.Slice(readOffset));
            } while (readOffset < buffer.Length);
        }

        private static async Task SerializeHeader(Stream stream, FileDatabaseHeader header)
        {
            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            header.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileDatabaseHeader> DeserializeHeader(Stream stream)
        {
            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileDatabaseHeader fileHeader = new FileDatabaseHeader();
            fileHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileHeader;
        }

        private static async Task SerializeEntryMetadataListHeader(Stream stream, FileEntryMetadataListHeader header)
        {
            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            header.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileEntryMetadataListHeader> DeserializeEntryMetadataListHeader(Stream stream)
        {
            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileEntryMetadataListHeader fileEntryMetadataListHeader = new FileEntryMetadataListHeader();
            fileEntryMetadataListHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileEntryMetadataListHeader;
        }

        private static async Task SerializeResourceMetadata(Stream stream, FileResourceMetadata metadata)
        {
            byte[] buffer = new byte[FileResourceMetadata.SerializedLength];
            metadata.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileResourceMetadata> DeserializeResourceMetadata(Stream stream)
        {
            byte[] buffer = new byte[FileResourceMetadata.SerializedLength];
            await FillBuffer(stream, buffer);

            FileResourceMetadata fileResourceMetadata = new FileResourceMetadata();
            fileResourceMetadata.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileResourceMetadata;
        }
    }
}
