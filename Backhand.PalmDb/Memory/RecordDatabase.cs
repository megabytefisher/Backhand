using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb.Memory
{
    public class RecordDatabase : Database, IPalmRecordDb
    {
        private readonly List<MemoryDatabaseRecord> _records = new();

        public RecordDatabase(PalmDbHeader sourceHeader) : base(sourceHeader)
        {
        }
        
        public Task<ICollection<uint>> GetRecordIdsAsync(CancellationToken cancellationToken = default)
        {
            uint[] ids = _records.Select(r => r.Header.Id).ToArray();
            return Task.FromResult<ICollection<uint>>(ids);
        }
        
        public async Task<PalmDbRecordHeader?> ReadRecordByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken = default)
        {
            if (index >= _records.Count)
                return null;
            
            MemoryDatabaseRecord record = _records[index];
            if (dataStream != null) await dataStream.WriteAsync(record.Data, cancellationToken);
            return record.Header;
        }

        public async Task<PalmDbRecordHeader> ReadRecordByIdAsync(uint id, Stream? dataStream, CancellationToken cancellationToken = default)
        {
            MemoryDatabaseRecord record = _records.Single(r => r.Header.Id == id);
            if (dataStream != null) await dataStream.WriteAsync(record.Data, cancellationToken);
            return record.Header;
        }

        public Task WriteRecordAsync(PalmDbRecordHeader header, Memory<byte> data, CancellationToken cancellationToken = default)
        {
            MemoryDatabaseRecord? record = _records.SingleOrDefault(r => r.Header.Id == header.Id);

            if (record == null)
            {
                record = new MemoryDatabaseRecord(header, data.Span);
                _records.Add(record);
            }
            else
            {
                record.Header = header with { };
                record.Data = data.ToArray();
            }
            
            return Task.CompletedTask;
        }
        
        public class MemoryDatabaseRecord
        {
            public PalmDbRecordHeader Header { get; set; }
            public byte[] Data { get; set; }

            public MemoryDatabaseRecord(PalmDbRecordHeader header, Span<byte> data)
            {
                Header = header with { };
                Data = data.ToArray();
            }
        }
    }
}