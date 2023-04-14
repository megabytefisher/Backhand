using Backhand.Common.BinarySerialization;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Backhand.Pdb.FileSerialization
{
    internal static class PdbSerialization
    {
        private static readonly PdbHeader BlankHeader = new();
        private static readonly PdbEntryListHeader BlankEntryListHeader = new();
        private static readonly PdbResourceMetadata BlankResourceMetadata = new();

        public static readonly int HeaderSize = BinarySerializer<PdbHeader>.GetSize(BlankHeader);
        public static readonly int HeaderPaddingSize = sizeof(byte) * 2;
        public static readonly int EntryListHeaderSize = BinarySerializer<PdbEntryListHeader>.GetSize(BlankEntryListHeader);
        public static readonly int ResourceMetadataSize = BinarySerializer<PdbResourceMetadata>.GetSize(BlankResourceMetadata);

        public static async Task FillBuffer(Stream stream, Memory<byte> buffer)
        {
            int bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.Slice(bytesRead)).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        public static async Task WriteHeaderAsync(Stream stream, PdbHeader header)
        {
            byte[] buffer = new byte[HeaderSize];
            BinarySerializer<PdbHeader>.Serialize(header, buffer);
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }

        public static async Task<PdbHeader> ReadHeaderAsync(Stream stream)
        {
            PdbHeader header = new();
            byte[] buffer = new byte[HeaderSize];
            await FillBuffer(stream, buffer).ConfigureAwait(false);
            BinarySerializer<PdbHeader>.Deserialize(new ReadOnlySequence<byte>(buffer), header);
            return header;
        }

        public static async Task WriteEntryListHeaderAsync(Stream stream, PdbEntryListHeader header)
        {
            byte[] buffer = new byte[EntryListHeaderSize];
            BinarySerializer<PdbEntryListHeader>.Serialize(header, buffer);
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }

        public static async Task<PdbEntryListHeader> ReaderEntryListHeaderAsync(Stream stream)
        {
            PdbEntryListHeader header = new();
            byte[] buffer = new byte[EntryListHeaderSize];
            await FillBuffer(stream, buffer).ConfigureAwait(false);
            BinarySerializer<PdbEntryListHeader>.Deserialize(new ReadOnlySequence<byte>(buffer), header);
            return header;
        }

        public static async Task WriteResourceMetadataAsync(Stream stream, PdbResourceMetadata metadata)
        {
            byte[] buffer = new byte[ResourceMetadataSize];
            BinarySerializer<PdbResourceMetadata>.Serialize(metadata, buffer);
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }

        public static async Task<PdbResourceMetadata> ReadResourceMetadataAsync(Stream stream)
        {
            PdbResourceMetadata metadata = new();
            byte[] buffer = new byte[ResourceMetadataSize];
            await FillBuffer(stream, buffer).ConfigureAwait(false);
            BinarySerializer<PdbResourceMetadata>.Deserialize(new ReadOnlySequence<byte>(buffer), metadata);
            return metadata;
        }
    }
}
