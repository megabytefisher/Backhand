using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Data;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.PalmDb.Dlp
{
    public class DlpRecordDatabase : DlpDatabase, IPalmRecordDb
    {
        public DlpRecordDatabase(DlpClient client, PalmDbHeader header, byte dbHandle) : base(client, header, dbHandle)
        {
        }

        public async Task<ICollection<uint>> GetRecordIdsAsync(CancellationToken cancellationToken = default)
        {
            const int recordIdsPerRequest = 50;

            List<uint> recordIds = new();
            for (ushort startIndex = 0;; startIndex = Convert.ToUInt16(recordIds.Count))
            {
                try
                {
                    ReadRecordIdListResponse listResponse = await Client.ReadRecordIdListAsync(new()
                    {
                        DbHandle = DbHandle,
                        Flags = 0,
                        MaxRecords = recordIdsPerRequest,
                        StartIndex = startIndex
                    }, cancellationToken).ConfigureAwait(false);
                    
                    recordIds.AddRange(listResponse.RecordIds);
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    break;
                }
            }
            
            return recordIds;
        }
        
        public async Task<PalmDbRecordHeader?> ReadRecordByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken = default)
        {
            ReadRecordResponse response;
            try
            {
                response = await Client.ReadRecordByIndexAsync(new()
                {
                    DbHandle = DbHandle,
                    RecordIndex = index,
                    MaxLength = dataStream != null ? ushort.MaxValue : (ushort)0
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
            {
                return null;
            }

            if (dataStream != null)
            {
                await dataStream.WriteAsync(response.Data, cancellationToken).ConfigureAwait(false);
            }

            return new PalmDbRecordHeader
            {
                Id = response.RecordId,
                Attributes = (DatabaseRecordAttributes)response.Attributes,
                Category = response.Category,
                Archive = false
            };
        }
        
        public async Task<PalmDbRecordHeader> ReadRecordByIdAsync(uint id, Stream? dataStream, CancellationToken cancellationToken = default)
        {
            ReadRecordResponse response = await Client.ReadRecordByIdAsync(new()
            {
                DbHandle = DbHandle,
                RecordId = id,
                MaxLength = dataStream != null ? ushort.MaxValue : (ushort)0
            }, cancellationToken).ConfigureAwait(false);

            if (dataStream != null)
            {
                await dataStream.WriteAsync(response.Data, cancellationToken).ConfigureAwait(false);
            }

            return new PalmDbRecordHeader
            {
                Id = response.RecordId,
                Attributes = (DatabaseRecordAttributes)response.Attributes,
                Category = response.Category,
                Archive = false
            };
        }

        public async Task WriteRecordAsync(PalmDbRecordHeader header, Memory<byte> data, CancellationToken cancellationToken = default)
        {
            await Client.WriteRecordAsync(new()
            {
                DbHandle = DbHandle,
                Attributes = (DlpRecordAttributes)header.Attributes,
                RecordId = header.Id,
                Category = header.Category,
                Data = data.ToArray()
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}