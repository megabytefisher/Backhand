using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.PalmDb.Memory;

namespace Backhand.PalmDb.FileIO
{
    public class DirectoryDbRepository : IPalmDbRepository
    {
        public DirectoryInfo Directory { get; }
        
        private readonly Dictionary<Database, FileInfo> _openDatabaseFiles = new();
        
        public DirectoryDbRepository(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public async Task<ICollection<PalmDbFileHeader>> GetHeadersAsync(CancellationToken cancellationToken = default)
        {
            List<PalmDbFileHeader> results = new();
            
            foreach (FileInfo file in Directory.EnumerateFiles())
            {
                string extension = file.Extension.ToLower();
                if (extension != ".pdb" && extension != ".prc")
                {
                    continue;
                }
                
                results.Add(await PalmDbFile.ReadHeaderAsync(file, cancellationToken).ConfigureAwait(false));
            }
            
            return results;
        }
        async Task<ICollection<PalmDbHeader>> IPalmDbRepository.GetHeadersAsync(CancellationToken cancellationToken)
            => (await GetHeadersAsync(cancellationToken).ConfigureAwait(false)).Cast<PalmDbHeader>().ToList();

        public async Task<IPalmDb> OpenDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            PalmDbFileHeader? fileHeader = header as PalmDbFileHeader;
            if (fileHeader == null)
            {
                throw new ArgumentException("The header must be a FileDatabaseHeader.", nameof(header));
            }

            await using FileStream stream = fileHeader.File.OpenRead();
            return await PalmDbFile.ReadAsync(stream, cancellationToken);
        }

        public Task<IPalmDb> CreateDatabaseAsync(string path, PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            FileInfo databaseFile = new(Path.Combine(Directory.FullName, path));

            if (databaseFile.Exists)
            {
                throw new IOException("The database file already exists.");
            }

            Database database;
            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                database = new ResourceDatabase(header);
            }
            else
            {
                database = new RecordDatabase(header);
            }
            
            _openDatabaseFiles.Add(database, databaseFile);
            return Task.FromResult<IPalmDb>(database);
        }

        public async Task<IPalmDb> CreateDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            string path = header.Attributes.HasFlag(DatabaseAttributes.ResourceDb) ? ".prc" : ".pdb";
            return await CreateDatabaseAsync(path, header, cancellationToken).ConfigureAwait(false);
        }

        public Task DeleteDatabaseAsync(PalmDbHeader header, CancellationToken cancellationToken = default)
        {
            PalmDbFileHeader? fileHeader = header as PalmDbFileHeader;
            if (fileHeader == null)
            {
                throw new System.ArgumentException("The header must be a FileDatabaseHeader.", nameof(header));
            }
            
            fileHeader.File.Delete();

            return Task.CompletedTask;
        }

        public async Task CloseDatabaseAsync(IPalmDb database, CancellationToken cancellationToken = default)
        {
            if (database is not Database memoryDatabase)
            {
                return;
            }
            
            if (!_openDatabaseFiles.TryGetValue(memoryDatabase, out FileInfo? file))
            {
                throw new System.ArgumentException("The database is not open.", nameof(database));
            }

            await PalmDbFile.WriteAsync(file, database, cancellationToken).ConfigureAwait(false);
            _openDatabaseFiles.Remove(memoryDatabase);
        }
    }
}