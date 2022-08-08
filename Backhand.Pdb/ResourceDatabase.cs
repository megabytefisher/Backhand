using Backhand.Pdb.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class ResourceDatabase : Database
    {
        public List<DatabaseResource> Resources { get; set; } = new List<DatabaseResource>();

        public override async Task Serialize(Stream stream)
        {
            uint appInfoOffset =
                FileDatabaseHeader.SerializedLength +
                FileEntryMetadataListHeader.SerializedLength +
                (FileResourceMetadata.SerializedLength * (uint)Resources.Count) +
                2;
            uint sortInfoOffset = appInfoOffset + (uint)(AppInfo?.Length ?? 0);
            uint resourceBlockOffset = sortInfoOffset + (uint)(SortInfo?.Length ?? 0);

            await SerializeHeader(stream,
                (AppInfo != null && AppInfo.Length > 0) ? appInfoOffset : 0,
                (SortInfo != null && SortInfo.Length > 0) ? sortInfoOffset : 0);

            await SerializeEntryMetadataListHeader(stream, Convert.ToUInt16(Resources.Count));

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

        public override async Task Deserialize(Stream stream)
        {
            (uint appInfoId, uint sortInfoId) = await DeserializeHeader(stream);

            ushort entryCount = await DeserializeEntryMetadataListHeader(stream);

            // Read each metadata entry
            List<FileResourceMetadata> metadataList = new List<FileResourceMetadata>();
            for (ushort i = 0; i < entryCount; i++)
            {
                metadataList.Add(await DeserializeResourceMetadata(stream));
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

                DatabaseResource resource = new DatabaseResource();
                resource.Type = metadata.Type;
                resource.ResourceId = metadata.ResourceId;
                resource.Data = resourceBuffer;
                Resources.Add(resource);
            }
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
