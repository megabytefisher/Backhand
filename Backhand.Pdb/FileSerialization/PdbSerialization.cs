using Backhand.Common.BinarySerialization;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Pdb.FileSerialization
{
    internal static class PdbSerialization
    {
        private static readonly PdbHeader BlankHeader = new();
        private static readonly PdbEntryListHeader BlankEntryListHeader = new();
        private static readonly PdbResourceMetadata BlankResourceMetadata = new();
        private static readonly PdbRecordMetadata BlankRecordMetadata = new();

        public static readonly int HeaderSize = BinarySerializer<PdbHeader>.GetSize(BlankHeader);
        public static readonly int HeaderPaddingSize = sizeof(byte) * 2;
        public static readonly int EntryListHeaderSize = BinarySerializer<PdbEntryListHeader>.GetSize(BlankEntryListHeader);
        public static readonly int ResourceMetadataSize = BinarySerializer<PdbResourceMetadata>.GetSize(BlankResourceMetadata);
        public static readonly int RecordMetadataSize = BinarySerializer<PdbRecordMetadata>.GetSize(BlankRecordMetadata);

        public static async Task FillBuffer(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.Slice(bytesRead), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        public static async Task WriteHeaderAsync(Stream stream, PdbHeader header, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[HeaderSize];
            BinarySerializer<PdbHeader>.Serialize(header, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<PdbHeader> ReadHeaderAsync(Stream stream, CancellationToken cancellationToken)
        {
            PdbHeader header = new();
            byte[] buffer = new byte[HeaderSize];
            await FillBuffer(stream, buffer, cancellationToken).ConfigureAwait(false);
            BinarySerializer<PdbHeader>.Deserialize(new ReadOnlySequence<byte>(buffer), header);
            return header;
        }

        public static async Task WriteEntryListHeaderAsync(Stream stream, PdbEntryListHeader header, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[EntryListHeaderSize];
            BinarySerializer<PdbEntryListHeader>.Serialize(header, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<PdbEntryListHeader> ReaderEntryListHeaderAsync(Stream stream, CancellationToken cancellationToken)
        {
            PdbEntryListHeader header = new();
            byte[] buffer = new byte[EntryListHeaderSize];
            await FillBuffer(stream, buffer, cancellationToken).ConfigureAwait(false);
            BinarySerializer<PdbEntryListHeader>.Deserialize(new ReadOnlySequence<byte>(buffer), header);
            return header;
        }

        public static async Task WriteResourceMetadataAsync(Stream stream, PdbResourceMetadata metadata, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[ResourceMetadataSize];
            BinarySerializer<PdbResourceMetadata>.Serialize(metadata, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<PdbResourceMetadata> ReadResourceMetadataAsync(Stream stream, CancellationToken cancellationToken)
        {
            PdbResourceMetadata metadata = new();
            byte[] buffer = new byte[ResourceMetadataSize];
            await FillBuffer(stream, buffer, cancellationToken).ConfigureAwait(false);
            BinarySerializer<PdbResourceMetadata>.Deserialize(new ReadOnlySequence<byte>(buffer), metadata);
            return metadata;
        }

        public static async Task WriteRecordMetadataAsync(Stream stream, PdbRecordMetadata metadata, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[RecordMetadataSize];
            BinarySerializer<PdbRecordMetadata>.Serialize(metadata, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<PdbRecordMetadata> ReadRecordMetadataAsync(Stream stream, CancellationToken cancellationToken)
        {
            PdbRecordMetadata metadata = new();
            byte[] buffer = new byte[RecordMetadataSize];
            await FillBuffer(stream, buffer, cancellationToken).ConfigureAwait(false);
            BinarySerializer<PdbRecordMetadata>.Deserialize(new ReadOnlySequence<byte>(buffer), metadata);
            return metadata;
        }
    }
}
