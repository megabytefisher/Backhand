using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.PalmDb.Dlp
{
    public class DlpDatabase : IPalmDb
    {
        public DlpConnection Connection { get; }
        public byte DbHandle { get; }
        
        private PalmDbHeader _header;
        
        public DlpDatabase(DlpConnection connection, PalmDbHeader header, byte dbHandle)
        {
            Connection = connection;
            DbHandle = dbHandle;
            _header = header;
        }

        public Task<PalmDbHeader> ReadHeaderAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_header);
        }
        
        public Task WriteHeaderAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task ReadAppInfoAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            try
            {
                ReadAppBlockResponse readResponse = await Connection.ReadAppBlockAsync(new()
                {
                    DbHandle = DbHandle,
                    Offset = 0,
                    Length = ushort.MaxValue
                }, cancellationToken).ConfigureAwait(false);

                await stream.WriteAsync(readResponse.Data, cancellationToken);
            }
            catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
            {
                return;
            }
        }
        
        public async Task WriteAppInfoAsync(Memory<byte> data, CancellationToken cancellationToken = default)
        {
            await Connection.WriteAppBlockAsync(new()
            {
                DbHandle = DbHandle,
                Data = data.ToArray()
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task ReadSortInfoAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            try
            {
                ReadSortBlockResponse readResponse = await Connection.ReadSortBlockAsync(new()
                {
                    DbHandle = DbHandle,
                    Offset = 0,
                    Length = ushort.MaxValue
                }, cancellationToken).ConfigureAwait(false);

                await stream.WriteAsync(readResponse.Data, cancellationToken);
            }
            catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
            {
                return;
            }
        }
        
        public async Task WriteSortInfoAsync(Memory<byte> data, CancellationToken cancellationToken = default)
        {
            await Connection.WriteSortBlockAsync(new()
            {
                DbHandle = DbHandle,
                Data = data.ToArray()
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}