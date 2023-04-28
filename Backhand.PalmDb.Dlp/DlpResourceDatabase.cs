using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.PalmDb.Dlp
{
    public class DlpResourceDatabase : DlpDatabase, IPalmResourceDb
    {
        public DlpResourceDatabase(DlpConnection connection, PalmDbHeader header, byte dbHandle) : base(connection, header, dbHandle)
        {
        }

        public async Task<PalmDbResourceHeader?> ReadResourceByIndexAsync(ushort index, Stream? dataStream, CancellationToken cancellationToken = default)
        {
            ReadResourceResponse response;
            try
            {
                response = await Connection.ReadResourceByIndexAsync(new()
                {
                    DbHandle = DbHandle,
                    ResourceIndex = index,
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

            return new PalmDbResourceHeader
            {
                Type = response.Type,
                Id = response.ResourceId
            };
        }

        public async Task WriteResourceAsync(PalmDbResourceHeader header, Memory<byte> data, CancellationToken cancellationToken)
        {
            await Connection.WriteResourceAsync(new()
            {
                DbHandle = DbHandle,
                Type = header.Type,
                ResourceId = header.Id,
                Data = data.ToArray()
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}