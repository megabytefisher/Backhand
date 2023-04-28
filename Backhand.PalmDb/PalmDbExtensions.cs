using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.PalmDb
{
    public static class PalmDbExtensions
    {
        public static async Task<IPalmDb> CopyToAsync(
            this IPalmDb db,
            IPalmDbRepository dbRepository,
            CancellationToken cancellationToken = default
        )
        {
            // Read source header
            PalmDbHeader header =
                await db.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            
            // Create new database
            IPalmDb newDb =
                await dbRepository.CreateDatabaseAsync(header, cancellationToken).ConfigureAwait(false);

            using MemoryStream bufferStream = new();
            
            // Copy app info
            await db.ReadAppInfoAsync(bufferStream, cancellationToken).ConfigureAwait(false);
            await newDb.WriteAppInfoAsync(
                bufferStream.GetBuffer().AsMemory(0, (int)bufferStream.Length),
                cancellationToken
            ).ConfigureAwait(false);

            bufferStream.Seek(0, SeekOrigin.Begin);
            bufferStream.SetLength(0);

            // Copy sort info
            await db.ReadSortInfoAsync(bufferStream, cancellationToken).ConfigureAwait(false);
            await newDb.WriteSortInfoAsync(
                bufferStream.GetBuffer().AsMemory(0, (int)bufferStream.Length),
                cancellationToken
            ).ConfigureAwait(false);
            
            if (db is IPalmResourceDb resourceDb)
            {
                IPalmResourceDb newResourceDb = (IPalmResourceDb)newDb;

                for (ushort index = 0;; index++)
                {
                    bufferStream.Seek(0, SeekOrigin.Begin);
                    bufferStream.SetLength(0);
                    
                    PalmDbResourceHeader? resourceHeader =
                        await resourceDb.ReadResourceByIndexAsync(
                            index,
                            bufferStream,
                            cancellationToken
                        ).ConfigureAwait(false);

                    if (resourceHeader is null)
                        break;
                    
                    await newResourceDb.WriteResourceAsync(
                        resourceHeader,
                        bufferStream.GetBuffer().AsMemory(0, (int)bufferStream.Length),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
            else if (db is IPalmRecordDb recordDb)
            {
                IPalmRecordDb newRecordDb = (IPalmRecordDb)newDb;
                
                for (ushort index = 0;; index++)
                {
                    bufferStream.Seek(0, SeekOrigin.Begin);
                    bufferStream.SetLength(0);
                    
                    PalmDbRecordHeader? recordHeader =
                        await recordDb.ReadRecordByIndexAsync(
                            index,
                            bufferStream,
                            cancellationToken
                        ).ConfigureAwait(false);

                    if (recordHeader is null)
                        break;
                    
                    await newRecordDb.WriteRecordAsync(
                        recordHeader,
                        bufferStream.GetBuffer().AsMemory(0, (int)bufferStream.Length),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported database type.");
            }
            
            return newDb;
        }
    }
}