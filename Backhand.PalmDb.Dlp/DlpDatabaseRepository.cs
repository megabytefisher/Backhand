using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Dlp.Commands.v1_0.Data;
using Backhand.Protocols.Dlp;

namespace Backhand.PalmDb.Dlp
{
    public class DlpDatabaseRepository : IPalmDbRepository
    {
        public DlpClient Client { get; }
        
        public DlpDatabaseRepository(DlpClient client)
        {
            Client = client;
        }
        
        public async Task<ICollection<PalmDbHeader>> GetHeadersAsync(CancellationToken cancellationToken = default)
        {
            List<DatabaseMetadata> dbMetadata = new();
            for (ushort index = 0;; index = Convert.ToUInt16(dbMetadata.Max(md => md.Index) + 1))
            {
                try
                {
                    ReadDbListResponse response = await Client.ReadDbListAsync(new()
                    {
                        Mode = ReadDbListRequest.ReadDbListMode.ListRam,
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
            OpenDbResponse openResponse = await Client.OpenDbAsync(new()
            {
                Name = header.Name,
                Mode = OpenDbRequest.OpenDbMode.Read
            }, cancellationToken).ConfigureAwait(false);
            
            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                return new DlpResourceDatabase(Client, header, openResponse.DbHandle);
            }
            else
            {
                return new DlpRecordDatabase(Client, header, openResponse.DbHandle);
            }
        }

        public async Task<IPalmDb> CreateDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            CreateDbResponse createResponse = await Client.CreateDbAsync(new()
            {
                Name = header.Name,
                Type = header.Type,
                Creator = header.Creator,
                Version = header.Version,
                Attributes = (DlpDatabaseAttributes)header.Attributes
            }, cancellationToken).ConfigureAwait(false);

            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                return new DlpResourceDatabase(Client, header, createResponse.DbHandle);
            }
            else
            {
                return new DlpRecordDatabase(Client, header, createResponse.DbHandle);
            }
        }

        public async Task DeleteDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            await Client.DeleteDbAsync(new()
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

            await Client.CloseDbAsync(new()
            {
                DbHandle = dlpDatabase.DbHandle
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}