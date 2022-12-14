using Backhand.Pdb.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class RecordDatabase : Database
    {
        public List<DatabaseRecord> Records { get; } = new();

        public override async Task SerializeAsync(Stream stream)
        {
            uint appInfoOffset =
                FileDatabaseHeader.SerializedLength +
                FileEntryMetadataListHeader.SerializedLength +
                (FileRecordMetadata.SerializedLength * (uint)Records.Count) +
                HeaderPaddingLength;
            uint sortInfoOffset = appInfoOffset + (uint)(AppInfo?.Length ?? 0);
            uint recordBlockOffset = sortInfoOffset + (uint)(SortInfo?.Length ?? 0);

            await SerializeHeaderAsync(stream,
                AppInfo is { Length: > 0 } ? appInfoOffset : 0,
                SortInfo is { Length: > 0 } ? sortInfoOffset : 0);

            await SerializeEntryMetadataListHeaderAsync(stream, Convert.ToUInt16(Records.Count));

            int blockOffset = 0;
            foreach (DatabaseRecord record in Records)
            {
                FileRecordMetadata metadata = new()
                {
                    Attributes = record.Attributes,
                    Category = record.Category,
                    Archive = record.Archive,
                    UniqueId = record.UniqueId,
                    LocalChunkId = (uint)(recordBlockOffset + blockOffset)
                };

                await SerializeRecordMetadataAsync(stream, metadata);

                blockOffset += record.Data.Length;
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

            // Write records...
            foreach (DatabaseRecord record in Records)
            {
                await stream.WriteAsync(record.Data);
            }
        }

        public override async Task DeserializeAsync(Stream stream)
        {
            (uint appInfoId, uint sortInfoId) = await DeserializeHeaderAsync(stream);

            ushort entryCount = await DeserializeEntryMetadataListHeaderAsync(stream);

            // Read each metadata entry
            List<FileRecordMetadata> metadataList = new();
            for (ushort i = 0; i < entryCount; i++)
            {
                metadataList.Add(await DeserializeRecordMetadata(stream));
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
            Records.Clear();
            foreach (FileRecordMetadata metadata in metadataList)
            {
                FileRecordMetadata? nextMetadata = metadataList
                    .Where(md => md.LocalChunkId > metadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint recordLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - metadata.LocalChunkId :
                    (uint)stream.Length - metadata.LocalChunkId;

                stream.Seek(metadata.LocalChunkId, SeekOrigin.Begin);
                byte[] recordBuffer = new byte[recordLength];
                await FillBuffer(stream, recordBuffer);

                DatabaseRecord record = new()
                {
                    Attributes = metadata.Attributes,
                    Category = metadata.Category,
                    Archive = metadata.Archive,
                    UniqueId = metadata.UniqueId,
                    Data = recordBuffer
                };
                Records.Add(record);
            }
        }

        private static async Task SerializeRecordMetadataAsync(Stream stream, FileRecordMetadata metadata)
        {
            byte[] buffer = new byte[FileRecordMetadata.SerializedLength];
            metadata.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileRecordMetadata> DeserializeRecordMetadata(Stream stream)
        {
            byte[] buffer = new byte[FileRecordMetadata.SerializedLength];
            await FillBuffer(stream, buffer);

            FileRecordMetadata fileRecordMetadata = new();
            fileRecordMetadata.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileRecordMetadata;
        }
    }
}
