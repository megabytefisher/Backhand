using Backhand.Dlp.Commands;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Dlp.Commands.v1_0.Data;
using Backhand.Protocols.Dlp;

namespace Backhand.PalmDb.Dlp
{
    public class DlpDatabaseRepository : IPalmDbRepository
    {
        public DlpConnection Connection { get; }
        
        public DlpDatabaseRepository(DlpConnection connection)
        {
            Connection = connection;
        }
        
        public async Task<ICollection<PalmDbHeader>> GetHeadersAsync(CancellationToken cancellationToken = default)
        {
            List<DatabaseMetadata> dbMetadata = new();
            for (ushort index = 0;; index = Convert.ToUInt16(dbMetadata.Count + 1))
            {
                try
                {
                    ReadDbListResponse response = await Connection.ReadDbListAsync(new()
                    {
                        Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                        StartIndex = index
                    }, cancellationToken).ConfigureAwait(false);
                    
                    dbMetadata.AddRange(response.Results);
                }
                catch (DlpCommandErrorException ex) when (ex.ErrorCode == DlpErrorCode.NotFoundError)
                {
                    break;
                }
            }
            
            return dbMetadata.Select(md => new PalmDbHeader
            {
                Name = md.Name,
                Attributes = (DatabaseAttributes)md.Attributes,
                Creator = md.Creator,
                Type = md.Type,
                Version = md.Version,
                ModificationNumber = md.ModificationNumber,
                CreationDate = md.CreationDate,
                ModificationDate = md.ModificationDate,
                LastBackupDate = md.LastBackupDate,
                UniqueIdSeed = 0
            }).ToList();
        }

        public async Task<IPalmDb> OpenDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            OpenDbResponse openResponse = await Connection.OpenDbAsync(new()
            {
                Name = header.Name,
                Mode = OpenDbRequest.OpenDbMode.Read
            }, cancellationToken).ConfigureAwait(false);
            
            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                return new DlpResourceDatabase(Connection, header, openResponse.DbHandle);
            }
            else
            {
                return new DlpRecordDatabase(Connection, header, openResponse.DbHandle);
            }
        }

        public async Task<IPalmDb> CreateDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            CreateDbResponse createResponse = await Connection.CreateDbAsync(new()
            {
                Name = header.Name,
                Type = header.Type,
                Creator = header.Creator,
                Version = header.Version,
                Attributes = (DlpDatabaseAttributes)header.Attributes
            }, cancellationToken).ConfigureAwait(false);

            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                return new DlpResourceDatabase(Connection, header, createResponse.DbHandle);
            }
            else
            {
                return new DlpRecordDatabase(Connection, header, createResponse.DbHandle);
            }
        }

        public async Task DeleteDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            await Connection.DeleteDbAsync(new()
            {
                Name = header.Name
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task CloseDatabaseAsync(IPalmDb database, CancellationToken cancellationToken = default)
        {
            DlpDatabase? dlpDatabase = database as DlpDatabase;
            if (dlpDatabase == null)
            {
                throw new InvalidOperationException("Database is not a DLP database");
            }

            await Connection.CloseDbAsync(new()
            {
                DbHandle = dlpDatabase.DbHandle
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}