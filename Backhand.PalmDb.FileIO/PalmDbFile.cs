using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common.BinarySerialization;
using Backhand.PalmDb.FileIO.FileSerialization;
using Backhand.PalmDb.Memory;

namespace Backhand.PalmDb.FileIO
{
    public static class PalmDbFile
    {
        private static readonly int PdbHeaderSize = BinarySerializer<PdbHeader>.GetDefaultSize<PdbHeader>();
        private static readonly int PdbEntryListHeaderSize = BinarySerializer<PdbEntryListHeader>.GetDefaultSize<PdbEntryListHeader>();
        private static readonly int PdbRecordMetadataSize = BinarySerializer<PdbRecordMetadata>.GetDefaultSize<PdbRecordMetadata>();
        private static readonly int PdbResourceMetadataSize = BinarySerializer<PdbResourceMetadata>.GetDefaultSize<PdbResourceMetadata>();

        private static readonly byte[] HeaderPadding = new byte[2];
        
        public static async Task<PalmDbFileHeader> ReadHeaderAsync(FileInfo file, CancellationToken cancellationToken = default)
        {
            await using FileStream stream = file.OpenRead();
            PdbHeader header = await ReadPdbHeaderAsync(stream, cancellationToken);

            return header.ToPalmDbFileHeader(file);
        }

        public static async Task<IPalmDb> ReadAsync(FileInfo file, CancellationToken cancellationToken = default)
        {
            await using FileStream stream = file.OpenRead();
            return await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAsync(FileInfo file, IPalmDb database, CancellationToken cancellationToken = default)
        {
            await using FileStream stream = file.OpenWrite();
            await WriteAsync(stream, database, cancellationToken).ConfigureAwait(false);
        }
        
        public static async Task<Database> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            PdbHeader header = await ReadPdbHeaderAsync(stream, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            if (header.Attributes.HasFlag(DatabaseAttributes.ResourceDb))
            {
                return await ReadResourceDatabaseAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ReadRecordDatabaseAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }
        
        public static async Task WriteAsync(Stream stream, IPalmDb database, CancellationToken cancellationToken = default)
        {
            if (database is IPalmResourceDb resourceDatabase)
            {
                await WriteResourceDatabaseAsync(stream, resourceDatabase, cancellationToken);
            }
            else if (database is IPalmRecordDb recordDatabase)
            {
                await WriteRecordDatabaseAsync(stream, recordDatabase, cancellationToken);
            }
            else
            {
                throw new ArgumentException("Database type not recognized.", nameof(database));
            }
        }

        public static string GetFileName(PalmDbHeader header)
        {
            string safeName = Path.GetInvalidFileNameChars().Aggregate(header.Name, (current, c) => current.Replace(c, '_'));
            return Path.ChangeExtension(safeName, header.Attributes.HasFlag(DatabaseAttributes.ResourceDb) ? ".prc" : ".pdb");
        }

        private static async Task<RecordDatabase> ReadRecordDatabaseAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Read the header and create a database in memory
            PdbHeader header = await ReadPdbHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            RecordDatabase database = new(header.ToPalmDbHeader());
            
            // Read the entry list header
            PdbEntryListHeader entryListHeader = await ReadPdbEntryListHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            if (entryListHeader.NextListId != 0)
            {
                throw new InvalidOperationException("Databases with multiple entry lists are not supported");
            }
            
            // Read each metadata entry
            PdbRecordMetadata[] metadata = new PdbRecordMetadata[entryListHeader.Length];
            for (ushort i = 0; i < entryListHeader.Length; i++)
            {
                metadata[i] = await ReadPdbRecordMetadataAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            
            // Read AppInfo
            if (header.AppInfoId != 0)
            {
                uint appInfoLength =
                    header.SortInfoId != 0 ? header.SortInfoId - header.AppInfoId :
                    metadata.Length > 0 ? metadata.Min(md => md.LocalChunkId) - header.AppInfoId :
                    Convert.ToUInt32(stream.Length) - header.AppInfoId;

                byte[] appInfo = new byte[appInfoLength];
                stream.Seek(header.AppInfoId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(appInfo, cancellationToken).ConfigureAwait(false);
                await database.WriteAppInfoAsync(appInfo, cancellationToken);
            }
            
            // Read SortInfo
            if (header.SortInfoId != 0)
            {
                uint sortInfoLength =
                    metadata.Length > 0 ? metadata.Min(md => md.LocalChunkId) - header.SortInfoId :
                    Convert.ToUInt32(stream.Length) - header.SortInfoId;
                
                byte[] sortInfo = new byte[sortInfoLength];
                stream.Seek(header.SortInfoId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(sortInfo, cancellationToken).ConfigureAwait(false);
                await database.WriteSortInfoAsync(sortInfo, cancellationToken);
            }
            
            // Read entries
            foreach (PdbRecordMetadata recordMetadata in metadata)
            {
                PdbRecordMetadata? nextMetadata = metadata
                    .Where(md => md.LocalChunkId > recordMetadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint recordLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - recordMetadata.LocalChunkId :
                    Convert.ToUInt32(stream.Length) - recordMetadata.LocalChunkId;

                byte[] recordData = new byte[recordLength];
                stream.Seek(recordMetadata.LocalChunkId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(recordData, cancellationToken).ConfigureAwait(false);
                await database.WriteRecordAsync(recordMetadata.ToPalmDbRecordHeader(), recordData, cancellationToken);
            }
            
            return database;
        }
        
        private static async Task WriteRecordDatabaseAsync(Stream stream, IPalmRecordDb database, CancellationToken cancellationToken)
        {
            // Read source database header
            PalmDbHeader sourceHeader = await database.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            
            // Read AppInfo/SortInfo from source database and store in memory
            using MemoryStream sourceAppInfo = new();
            using MemoryStream sourceSortInfo = new();
            await database.ReadAppInfoAsync(sourceAppInfo, cancellationToken).ConfigureAwait(false);
            await database.ReadSortInfoAsync(sourceSortInfo, cancellationToken).ConfigureAwait(false);
            sourceAppInfo.Seek(0, SeekOrigin.Begin);
            sourceSortInfo.Seek(0, SeekOrigin.Begin);
            
            // Read source records
            using MemoryStream recordData = new();
            List<PdbRecordMetadata> metadataList = new();
            for (ushort i = 0;; i++)
            {
                // Read record (writing to the recordData buffer)
                uint recordDataOffset = Convert.ToUInt32(recordData.Position);
                PalmDbRecordHeader? sourceRecordHeader = await database.ReadRecordByIndexAsync(i, recordData, cancellationToken).ConfigureAwait(false);

                if (sourceRecordHeader == null)
                    break;

                PdbRecordMetadata newMetadata = new(sourceRecordHeader, recordDataOffset);
                metadataList.Add(newMetadata);
            }
            recordData.Seek(0, SeekOrigin.Begin);

            // Calculate offsets for file
            uint appInfoOffset = Convert.ToUInt32(PdbHeaderSize) +
                                 Convert.ToUInt32(PdbEntryListHeaderSize) +
                                 Convert.ToUInt32(PdbRecordMetadataSize * metadataList.Count) +
                                 Convert.ToUInt32(HeaderPadding.Length);
            uint sortInfoOffset = appInfoOffset + Convert.ToUInt32(sourceAppInfo.Length);
            uint firstEntryOffset = sortInfoOffset + Convert.ToUInt32(sourceSortInfo.Length);
            
            if (sourceAppInfo.Length == 0) appInfoOffset = 0;
            if (sourceSortInfo.Length == 0) sortInfoOffset = 0;
            
            // Write file header
            await WritePdbHeaderAsync(stream, new PdbHeader(sourceHeader, appInfoOffset, sortInfoOffset), cancellationToken).ConfigureAwait(false);
            
            // Write entry list header
            await WritePdbEntryListHeaderAsync(stream, new PdbEntryListHeader(Convert.ToUInt16(metadataList.Count)), cancellationToken).ConfigureAwait(false);
            
            // Write each metadata
            foreach (PdbRecordMetadata metadata in metadataList)
            {
                metadata.LocalChunkId += firstEntryOffset;
                await WritePdbRecordMetadataAsync(stream, metadata, cancellationToken).ConfigureAwait(false);
            }
            
            // Write header padding
            await stream.WriteAsync(HeaderPadding, cancellationToken).ConfigureAwait(false);
            
            // Write AppInfo
            await sourceAppInfo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            
            // Write SortInfo
            await sourceSortInfo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            
            // Write records
            await recordData.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ResourceDatabase> ReadResourceDatabaseAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Read the header and create a database in memory
            PdbHeader header = await ReadPdbHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            ResourceDatabase database = new(header.ToPalmDbHeader());
            
            // Read the entry list header
            PdbEntryListHeader entryListHeader = await ReadPdbEntryListHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            if (entryListHeader.NextListId != 0)
            {
                throw new InvalidOperationException("Databases with multiple entry lists are not supported");
            }
            
            // Read each metadata entry
            PdbResourceMetadata[] metadata = new PdbResourceMetadata[entryListHeader.Length];
            for (ushort i = 0; i < entryListHeader.Length; i++)
            {
                metadata[i] = await ReadPdbResourceMetadataAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            
            // Read AppInfo
            if (header.AppInfoId != 0)
            {
                uint appInfoLength =
                    header.SortInfoId != 0 ? header.SortInfoId - header.AppInfoId :
                    metadata.Length > 0 ? metadata.Min(md => md.LocalChunkId) - header.AppInfoId :
                    Convert.ToUInt32(stream.Length) - header.AppInfoId;

                byte[] appInfo = new byte[appInfoLength];
                stream.Seek(header.AppInfoId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(appInfo, cancellationToken).ConfigureAwait(false);
                await database.WriteAppInfoAsync(appInfo, cancellationToken);
            }
            
            // Read SortInfo
            if (header.SortInfoId != 0)
            {
                uint sortInfoLength =
                    metadata.Length > 0 ? metadata.Min(md => md.LocalChunkId) - header.SortInfoId :
                    Convert.ToUInt32(stream.Length) - header.SortInfoId;

                byte[] sortInfo = new byte[sortInfoLength];
                stream.Seek(header.SortInfoId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(sortInfo, cancellationToken).ConfigureAwait(false);
                await database.WriteSortInfoAsync(sortInfo, cancellationToken);
            }
            
            // Read entries
            foreach (PdbResourceMetadata resourceMetadata in metadata)
            {
                PdbResourceMetadata? nextMetadata = metadata
                    .Where(md => md.LocalChunkId > resourceMetadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint recordLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - resourceMetadata.LocalChunkId :
                        Convert.ToUInt32(stream.Length) - resourceMetadata.LocalChunkId;

                byte[] recordData = new byte[recordLength];
                stream.Seek(resourceMetadata.LocalChunkId, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(recordData, cancellationToken).ConfigureAwait(false);
                await database.WriteResourceAsync(resourceMetadata.ToPalmDbResourceHeader(), recordData, cancellationToken);
            }

            return database;
        }
        
        private static async Task WriteResourceDatabaseAsync(Stream stream, IPalmResourceDb database, CancellationToken cancellationToken)
        {
            // Read source database header
            PalmDbHeader sourceHeader = await database.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            
            // Read AppInfo/SortInfo from source database and store in memory
            using MemoryStream sourceAppInfo = new();
            using MemoryStream sourceSortInfo = new();
            await database.ReadAppInfoAsync(sourceAppInfo, cancellationToken).ConfigureAwait(false);
            await database.ReadSortInfoAsync(sourceSortInfo, cancellationToken).ConfigureAwait(false);
            sourceAppInfo.Seek(0, SeekOrigin.Begin);
            sourceSortInfo.Seek(0, SeekOrigin.Begin);
            
            // Read source resources
            using MemoryStream resourceData = new();
            List<PdbResourceMetadata> metadataList = new();
            for (ushort i = 0;; i++)
            {
                // Read resource (writing to the resourceData buffer)
                uint resourceDataOffset = Convert.ToUInt32(resourceData.Position);
                PalmDbResourceHeader? sourceResourceHeader = await database.ReadResourceByIndexAsync(i, resourceData, cancellationToken).ConfigureAwait(false);

                if (sourceResourceHeader == null)
                    break;

                PdbResourceMetadata newMetadata = new(sourceResourceHeader, resourceDataOffset);
                metadataList.Add(newMetadata);
            }
            resourceData.Seek(0, SeekOrigin.Begin);

            // Calculate offsets for file
            uint appInfoOffset = Convert.ToUInt32(PdbHeaderSize) +
                                 Convert.ToUInt32(PdbEntryListHeaderSize) +
                                 Convert.ToUInt32(PdbResourceMetadataSize * metadataList.Count) +
                                 Convert.ToUInt32(HeaderPadding.Length);
            uint sortInfoOffset = appInfoOffset + Convert.ToUInt32(sourceAppInfo.Length);
            uint firstEntryOffset = sortInfoOffset + Convert.ToUInt32(sourceSortInfo.Length);
            
            if (sourceAppInfo.Length == 0) appInfoOffset = 0;
            if (sourceSortInfo.Length == 0) sortInfoOffset = 0;
            
            // Write file header
            await WritePdbHeaderAsync(stream, new PdbHeader(sourceHeader, appInfoOffset, sortInfoOffset), cancellationToken).ConfigureAwait(false);
            
            // Write entry list header
            await WritePdbEntryListHeaderAsync(stream, new PdbEntryListHeader(Convert.ToUInt16(metadataList.Count)), cancellationToken).ConfigureAwait(false);
            
            // Write each metadata
            foreach (PdbResourceMetadata metadata in metadataList)
            {
                metadata.LocalChunkId += firstEntryOffset;
                await WritePdbResourceMetadataAsync(stream, metadata, cancellationToken).ConfigureAwait(false);
            }
            
            // Write header padding
            await stream.WriteAsync(HeaderPadding, cancellationToken).ConfigureAwait(false);
            
            // Write AppInfo
            await sourceAppInfo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            
            // Write SortInfo
            await sourceSortInfo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            
            // Write resources
            await resourceData.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<PdbHeader> ReadPdbHeaderAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbHeaderSize];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            PdbHeader header = new();
            BinarySerializer<PdbHeader>.Deserialize(buffer, header);
            return header;
        }

        private static async Task WritePdbHeaderAsync(Stream stream, PdbHeader header, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbHeaderSize];
            BinarySerializer<PdbHeader>.Serialize(header, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        
        private static async Task<PdbEntryListHeader> ReadPdbEntryListHeaderAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbEntryListHeaderSize];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            PdbEntryListHeader entryListHeader = new();
            BinarySerializer<PdbEntryListHeader>.Deserialize(buffer, entryListHeader);
            return entryListHeader;
        }
        
        private static async Task WritePdbEntryListHeaderAsync(Stream stream, PdbEntryListHeader entryListHeader, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbEntryListHeaderSize];
            BinarySerializer<PdbEntryListHeader>.Serialize(entryListHeader, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        
        private static async Task<PdbRecordMetadata> ReadPdbRecordMetadataAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbRecordMetadataSize];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            PdbRecordMetadata recordMetadata = new();
            BinarySerializer<PdbRecordMetadata>.Deserialize(buffer, recordMetadata);
            return recordMetadata;
        }
        
        private static async Task WritePdbRecordMetadataAsync(Stream stream, PdbRecordMetadata recordMetadata, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbRecordMetadataSize];
            BinarySerializer<PdbRecordMetadata>.Serialize(recordMetadata, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        
        private static async Task<PdbResourceMetadata> ReadPdbResourceMetadataAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbResourceMetadataSize];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            PdbResourceMetadata resourceMetadata = new();
            BinarySerializer<PdbResourceMetadata>.Deserialize(buffer, resourceMetadata);
            return resourceMetadata;
        }
        
        private static async Task WritePdbResourceMetadataAsync(Stream stream, PdbResourceMetadata resourceMetadata, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[PdbResourceMetadataSize];
            BinarySerializer<PdbResourceMetadata>.Serialize(resourceMetadata, buffer);
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
    }
}