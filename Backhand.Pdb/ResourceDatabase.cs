using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using Backhand.Pdb.FileSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class ResourceDatabase : Database
    {
        public List<DatabaseResource> Resources { get; } = new();

        public override async Task SerializeAsync(Stream stream)
        {
            uint appInfoOffset =
                Convert.ToUInt32(BinarySerializer<DatabaseFileHeader>.GetSize(null)) +
                Convert.ToUInt32(BinarySerializer<DatabaseFileEntryListHeader>.GetSize(null)) +
                Convert.ToUInt32(BinarySerializer<DatabaseFileResourceMetadata>.GetSize(null) * Resources.Count) +
                HeaderPaddingLength;
            uint sortInfoOffset = appInfoOffset + Convert.ToUInt32(AppInfo?.Length ?? 0);
            uint resourceBlockOffset = sortInfoOffset + Convert.ToUInt32(SortInfo?.Length ?? 0);

            await WriteHeaderAsync(
                stream,
                AppInfo is { Length: > 0 } ? appInfoOffset : 0,
                SortInfo is { Length: > 0 } ? sortInfoOffset : 0).ConfigureAwait(false);

            await WriteEntryListHeaderAsync(stream, Convert.ToUInt16(Resources.Count)).ConfigureAwait(false);

            uint blockOffset = resourceBlockOffset;
            foreach (DatabaseResource resource in Resources)
            {
                await WriteResourceMetadataAsync(stream, new DatabaseFileResourceMetadata
                {
                    Type = resource.Type,
                    ResourceId = resource.ResourceId,
                    LocalChunkId = blockOffset
                }).ConfigureAwait(false);

                blockOffset += Convert.ToUInt32(resource.Data.Length);
            }

            // Write header padding
            await stream.WriteAsync(new byte[HeaderPaddingLength]).ConfigureAwait(false);

            // Write AppInfo
            if (AppInfo is { Length: > 0 })
            {
                await stream.WriteAsync(AppInfo).ConfigureAwait(false);
            }

            // Write SortInfo
            if (SortInfo is { Length: > 0 })
            {
                await stream.WriteAsync(SortInfo).ConfigureAwait(false);
            }

            // Write Resources
            foreach (DatabaseResource resource in Resources)
            {
                await stream.WriteAsync(resource.Data).ConfigureAwait(false);
            }
        }

        public override async Task DeserializeAsync(Stream stream)
        {
            (uint appInfoId, uint sortInfoId) = await ReadHeaderAsync(stream);
            ushort entryCount = await ReadEntryListHeaderAsync(stream);
        }

        private static async Task WriteResourceMetadataAsync(Stream stream, DatabaseFileResourceMetadata metadata)
        {
            byte[] buffer = new byte[BinarySerializer<DatabaseFileResourceMetadata>.GetSize(metadata)];
            WriteResourceMetadata(metadata, buffer);
            await stream.WriteAsync(buffer);
        }

        private static async Task ReadResourceMetadataAsync(Stream stream, DatabaseFileResourceMetadata metadata)
        {

        }
        
        private static void WriteResourceMetadata(DatabaseFileResourceMetadata metadata, Span<byte> buffer)
        {
            SpanWriter<byte> bufferWriter = new SpanWriter<byte>(buffer);
            BinarySerializer<DatabaseFileResourceMetadata>.Serialize(metadata, ref bufferWriter);
        }
    }
}
