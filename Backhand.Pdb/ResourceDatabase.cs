using Backhand.Pdb.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class ResourceDatabase : Database
    {
        public List<DatabaseResource> Resources { get; } = new();

        public override async Task SerializeAsync(Stream stream)
        {
            uint appInfoOffset =
                FileDatabaseHeader.SerializedLength +
                FileEntryMetadataListHeader.SerializedLength +
                (FileResourceMetadata.SerializedLength * (uint)Resources.Count) +
                HeaderPaddingLength;
            uint sortInfoOffset = appInfoOffset + (uint)(AppInfo?.Length ?? 0);
            uint resourceBlockOffset = sortInfoOffset + (uint)(SortInfo?.Length ?? 0);

            await SerializeHeaderAsync(stream,
                AppInfo is { Length: > 0 } ? appInfoOffset : 0,
                SortInfo is { Length: > 0 } ? sortInfoOffset : 0);

            await SerializeEntryMetadataListHeaderAsync(stream, Convert.ToUInt16(Resources.Count));

            int blockOffset = 0;
            foreach (DatabaseResource resource in Resources)
            {
                FileResourceMetadata metadata = new()
                {
                    Type = resource.Type,
                    ResourceId = resource.ResourceId,
                    LocalChunkId = (uint)(resourceBlockOffset + blockOffset)
                };

                await SerializeResourceMetadataAsync(stream, metadata);

                blockOffset += resource.Data.Length;
            }

            // Write padding
            await stream.WriteAsync(new byte[HeaderPaddingLength]);

            // Write AppInfo
            if (AppInfo is { Length: > 0 })
            {
                await stream.WriteAsync(AppInfo);
            }

            // Write SortInfo
            if (SortInfo is { Length: > 0 })
            {
                await stream.WriteAsync(SortInfo);
            }

            // Write resources...
            foreach (DatabaseResource resource in Resources)
            {
                await stream.WriteAsync(resource.Data);
            }
        }

        public override async Task DeserializeAsync(Stream stream)
        {
            (uint appInfoId, uint sortInfoId) = await DeserializeHeaderAsync(stream);

            ushort entryCount = await DeserializeEntryMetadataListHeaderAsync(stream);

            // Read each metadata entry
            List<FileResourceMetadata> metadataList = new();
            for (ushort i = 0; i < entryCount; i++)
            {
                metadataList.Add(await DeserializeResourceMetadataAsync(stream));
            }

            // Read AppInfo block
            if (appInfoId != 0)
            {
                uint appInfoLength =
                    sortInfoId != 0 ? sortInfoId - appInfoId :
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - appInfoId :
                    (uint)stream.Length - appInfoId;

                stream.Seek(appInfoId, SeekOrigin.Begin);
                AppInfo = new byte[appInfoLength];
                await FillBuffer(stream, AppInfo);
            }

            // Read SortInfo block
            if (sortInfoId != 0)
            {
                uint sortInfoLength =
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - sortInfoId :
                    (uint)stream.Length - sortInfoId;

                stream.Seek(sortInfoId, SeekOrigin.Begin);
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

                DatabaseResource resource = new()
                {
                    Type = metadata.Type,
                    ResourceId = metadata.ResourceId,
                    Data = resourceBuffer
                };
                Resources.Add(resource);
            }
        }

        private static async Task SerializeResourceMetadataAsync(Stream stream, FileResourceMetadata metadata)
        {
            byte[] buffer = new byte[FileResourceMetadata.SerializedLength];
            metadata.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileResourceMetadata> DeserializeResourceMetadataAsync(Stream stream)
        {
            byte[] buffer = new byte[FileResourceMetadata.SerializedLength];
            await FillBuffer(stream, buffer);

            FileResourceMetadata fileResourceMetadata = new();
            fileResourceMetadata.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileResourceMetadata;
        }
    }
}
