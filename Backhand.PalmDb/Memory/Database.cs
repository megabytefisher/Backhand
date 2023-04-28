using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb.Memory
{
    public abstract class Database : IPalmDb
    {
        protected PalmDbHeader Header { get; set; }

        protected byte[] AppInfo { get; set; } = Array.Empty<byte>();
        protected byte[] SortInfo { get; set; } = Array.Empty<byte>();

        protected Database(PalmDbHeader sourceHeader)
        {
            Header = sourceHeader with { };
        }
        
        public Task<PalmDbHeader> ReadHeaderAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Header);
        }
        
        public Task WriteHeaderAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            Header = header with { };
            
            return Task.CompletedTask;
        }

        public async Task ReadAppInfoAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await stream.WriteAsync(AppInfo, cancellationToken).ConfigureAwait(false);
        }

        public Task WriteAppInfoAsync(Memory<byte> data, CancellationToken cancellationToken = default)
        {
            AppInfo = data.ToArray();
            return Task.CompletedTask;
        }

        public async Task ReadSortInfoAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await stream.WriteAsync(SortInfo, cancellationToken).ConfigureAwait(false);
        }

        public Task WriteSortInfoAsync(Memory<byte> data, CancellationToken cancellationToken = default)
        {
            SortInfo = data.ToArray();
            return Task.CompletedTask;
        }
    }
}