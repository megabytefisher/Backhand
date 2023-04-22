using Backhand.Common.Buffers;
using Backhand.Pdb.FileSerialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class RecordDatabase<TRecord> : Database where TRecord : DatabaseRecord, new()
    {
        public virtual List<TRecord> Records { get; } = new();

        public override async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            uint appInfoOffset =
                Convert.ToUInt32(PdbSerialization.HeaderSize) +
                Convert.ToUInt32(PdbSerialization.EntryListHeaderSize) +
                Convert.ToUInt32(PdbSerialization.RecordMetadataSize * Records.Count) +
                Convert.ToUInt32(PdbSerialization.HeaderPaddingSize);
            uint sortInfoOffset = appInfoOffset + Convert.ToUInt32(AppInfo?.Length ?? 0);
            uint recordBlockOffset = sortInfoOffset + Convert.ToUInt32(SortInfo?.Length ?? 0);

            PdbHeader header = new();
            WriteFileHeader(
                header,
                AppInfo is { Length: > 0 } ? appInfoOffset : 0,
                SortInfo is { Length: > 0 } ? sortInfoOffset : 0);

            await PdbSerialization.WriteHeaderAsync(stream, header, cancellationToken).ConfigureAwait(false);

            PdbEntryListHeader entryListHeader = new PdbEntryListHeader
            {
                NextListId = 0,
                Length = Convert.ToUInt16(Records.Count)
            };

            await PdbSerialization.WriteEntryListHeaderAsync(stream, entryListHeader, cancellationToken).ConfigureAwait(false);

            uint blockOffset = recordBlockOffset;
            foreach (TRecord record in Records)
            {
                PdbRecordMetadata recordMetadata = new PdbRecordMetadata
                {
                    Attributes = record.Attributes,
                    Category = record.Category,
                    Archive = record.Archive,
                    UniqueId = record.UniqueId,
                    LocalChunkId = blockOffset
                };

                await PdbSerialization.WriteRecordMetadataAsync(stream, recordMetadata, cancellationToken).ConfigureAwait(false);
                blockOffset += Convert.ToUInt32(record.GetDataSize());
            }

            // Write header padding
            await stream.WriteAsync(new byte[PdbSerialization.HeaderPaddingSize]).ConfigureAwait(false);

            // Write AppInfo
            if (AppInfo is { Length: > 0 })
            {
                await stream.WriteAsync(AppInfo).ConfigureAwait(false);
            }

            // Write SortInfo
            if (SortInfo is { Length: > 0 })
            {
                await stream.WriteAsync(SortInfo).ConfigureAwait(false);
            }

            // Write records
            foreach (TRecord record in Records)
            {
                byte[] recordBuffer = new byte[record.GetDataSize()];
                record.WriteData(recordBuffer);
                await stream.WriteAsync(recordBuffer).ConfigureAwait(false);
            }
        }

        public override async Task DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            PdbHeader header = await PdbSerialization.ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            ReadFileHeader(header);

            PdbEntryListHeader entryListHeader = await PdbSerialization.ReaderEntryListHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            
            if (entryListHeader.NextListId != 0)
            {
                throw new Exception("PDB files with more than 1 entry list are not supported");
            }

            // Read each metadata entry
            List<PdbRecordMetadata> metadataList = new();
            for (ushort i = 0; i < entryListHeader.Length; i++)
            {
                metadataList.Add(await PdbSerialization.ReadRecordMetadataAsync(stream, cancellationToken).ConfigureAwait(false));
            }

            // Read AppInfo
            if (header.AppInfoId != 0)
            {
                uint appInfoLength =
                    header.SortInfoId != 0 ? header.SortInfoId - header.AppInfoId :
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - header.AppInfoId :
                    Convert.ToUInt32(stream.Length) - header.AppInfoId;

                stream.Seek(header.AppInfoId, SeekOrigin.Begin);
                AppInfo = new byte[appInfoLength];
                await stream.FillBufferAsync(AppInfo, cancellationToken);
            }

            // Read SortInfo
            if (header.SortInfoId != 0)
            {
                uint sortInfoLength =
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - header.SortInfoId :
                    Convert.ToUInt32(stream.Length) - header.SortInfoId;

                stream.Seek(header.SortInfoId, SeekOrigin.Begin);
                SortInfo = new byte[sortInfoLength];
                await stream.FillBufferAsync(SortInfo, cancellationToken);
            }

            // Read Resource entries
            Records.Clear();
            foreach (PdbRecordMetadata metadata in metadataList)
            {
                PdbRecordMetadata? nextMetadata = metadataList
                    .Where(md => md.LocalChunkId > metadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint recordLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - metadata.LocalChunkId :
                    Convert.ToUInt32(stream.Length) - metadata.LocalChunkId;

                stream.Seek(metadata.LocalChunkId, SeekOrigin.Begin);
                byte[] recordData = new byte[recordLength];
                await stream.FillBufferAsync(recordData, cancellationToken);

                TRecord newRecord = new TRecord();
                newRecord.ReadFileMetadata(metadata);
                newRecord.ReadData(new System.Buffers.ReadOnlySequence<byte>(recordData));

                Records.Add(newRecord);
            }
        }
    }

    public class RecordDatabase : RecordDatabase<RawDatabaseRecord>
    {
    }
}