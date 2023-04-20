using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Common.Buffers
{
    public static class StreamExtensions
    {
        public static async Task FillBufferAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
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
    }
}
