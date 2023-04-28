using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common.BinarySerialization;

namespace Backhand.PalmDb
{
    public static class PalmResourceDbExtensions
    {
        public static async Task<(PalmDbResourceHeader, T data)?> ReadResourceByIndexAsync<T>(
            this IPalmResourceDb db,
            ushort index,
            CancellationToken cancellationToken = default
        ) where T : IBinarySerializable, new()
        {
            using MemoryStream dataStream = new();
            PalmDbResourceHeader? header =
                await db.ReadResourceByIndexAsync(index, dataStream, cancellationToken).ConfigureAwait(false);

            if (header == null) return null;
            
            T result = new();
            BinarySerializer<T>.Deserialize(
                new ReadOnlySequence<byte>(dataStream.GetBuffer(), 0, Convert.ToInt32(dataStream.Length)),
                result);
            return (header, result);
        }

        public static async Task WriteResourceAsync<T>(
            this IPalmResourceDb db,
            PalmDbResourceHeader header,
            T data,
            CancellationToken cancellationToken = default
        ) where T : IBinarySerializable
        {
            int dataSize = BinarySerializer<T>.GetSize(data);
            byte[] serializedData = new byte[dataSize];
            BinarySerializer<T>.Serialize(data, serializedData);
            await db.WriteResourceAsync(header, serializedData, cancellationToken);
        }
    }
}