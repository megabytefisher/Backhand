using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common.BinarySerialization;

namespace Backhand.PalmDb
{
    public static class PalmRecordDbExtensions
    {
        public static async Task<(PalmDbRecordHeader, T data)?> ReadRecordByIndexAsync<T>(
            this IPalmRecordDb db,
            ushort index,
            CancellationToken cancellationToken = default
        ) where T : IBinarySerializable, new()
        {
            using MemoryStream dataStream = new();
            PalmDbRecordHeader? header =
                await db.ReadRecordByIndexAsync(index, dataStream, cancellationToken).ConfigureAwait(false);

            if (header == null) return null;
            
            T result = new();
            BinarySerializer<T>.Deserialize(
                new ReadOnlySequence<byte>(dataStream.GetBuffer(), 0, Convert.ToInt32(dataStream.Length)),
                result);
            return (header, result);
        }
        
        public static async Task<(PalmDbRecordHeader header, T data)> ReadRecordByIdAsync<T>(
            this IPalmRecordDb db,
            uint id,
            CancellationToken cancellationToken = default
        ) where T : IBinarySerializable, new()
        {
            using MemoryStream dataStream = new();
            PalmDbRecordHeader header = await db.ReadRecordByIdAsync(id, dataStream, cancellationToken).ConfigureAwait(false);
            
            T result = new();
            BinarySerializer<T>.Deserialize(
                new ReadOnlySequence<byte>(dataStream.GetBuffer(), 0, Convert.ToInt32(dataStream.Length)),
                result);
            return (header, result);
        }

        public static async Task WriteRecordAsync<T>(
            this IPalmRecordDb db,
            PalmDbRecordHeader header,
            T data,
            CancellationToken cancellationToken = default
        ) where T : IBinarySerializable
        {
            int dataSize = BinarySerializer<T>.GetSize(data);
            byte[] serializedData = new byte[dataSize];
            BinarySerializer<T>.Serialize(data, serializedData);
            await db.WriteRecordAsync(header, serializedData, cancellationToken);
        }
    }
}