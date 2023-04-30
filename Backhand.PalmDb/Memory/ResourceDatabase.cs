using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb.Memory
{
    public class ResourceDatabase : Database, IPalmResourceDb
    {
        private readonly List<MemoryDatabaseResource> _resources = new();
        
        public ResourceDatabase(PalmDbHeader sourceHeader) : base(sourceHeader)
        {
        }

        public async Task<PalmDbResourceHeader?> ReadResourceByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken)
        {
            if (index >= _resources.Count)
                return null;

            MemoryDatabaseResource resource = _resources[index];
            if (dataStream != null) await dataStream.WriteAsync(resource.Data, cancellationToken);
            return resource.Header;
        }

        public Task WriteResourceAsync(PalmDbResourceHeader header, Memory<byte> data, CancellationToken cancellationToken)
        {
            MemoryDatabaseResource? resource =
                _resources.FirstOrDefault(r => r.Header.Type == header.Type && r.Header.Id == header.Id);

            if (resource == null)
            {
                resource = new MemoryDatabaseResource(header, data.Span);
                _resources.Add(resource);
            }
            else
            {
                resource.Header = header with { };
                resource.Data = data.ToArray();
            }
            
            return Task.CompletedTask;
        }
        
        public class MemoryDatabaseResource
        {
            public PalmDbResourceHeader Header { get; set; }
            public byte[] Data { get; set; }
            
            public MemoryDatabaseResource(PalmDbResourceHeader header, Span<byte> data)
            {
                Header = header with { };
                Data = data.ToArray();
            }
        }
    }
}