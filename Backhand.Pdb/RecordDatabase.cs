using Backhand.Pdb.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class RecordDatabase
    {
        public DatabaseHeader Header { get; set; } = new DatabaseHeader();
        public byte[]? AppInfo { get; set; }
        public byte[]? SortInfo { get; set; }
        public List<DatabaseRecord> Records { get; set; } = new List<DatabaseRecord>();

        public async Task Serialize(Stream stream)
        {
            int appInfoOffset =
                FileDatabaseHeader.SerializedLength +
                FileEntryMetadataListHeader.SerializedLength +
                (FileRecordMetadata.SerializedLength * Records.Count) +
                2;
            int sortInfoOffset = appInfoOffset + (AppInfo?.Length ?? 0);
            int recordBlockOffset = sortInfoOffset + (SortInfo?.Length ?? 0);

            FileDatabaseHeader fileHeader = new FileDatabaseHeader();
            fileHeader.Name = Header.Name;
            fileHeader.Attributes = Header.Attributes;
            fileHeader.Version = Header.Version;
            fileHeader.CreationDate = Header.CreationDate;
            fileHeader.ModificationDate = Header.ModificationDate;
            fileHeader.LastBackupDate = Header.LastBackupDate;
            fileHeader.ModificationNumber = Header.ModificationNumber;
            fileHeader.AppInfoId = (uint)(AppInfo != null && AppInfo.Length > 0 ? appInfoOffset : 0);
            fileHeader.SortInfoId = (uint)(SortInfo != null && SortInfo.Length > 0 ? sortInfoOffset : 0);
            fileHeader.Type = Header.Type;
            fileHeader.Creator = Header.Creator;
            fileHeader.UniqueIdSeed = Header.UniqueIdSeed;

            await SerializeHeader(stream, fileHeader);

            FileEntryMetadataListHeader metadataListHeader = new FileEntryMetadataListHeader();
            metadataListHeader.NextListId = 0;
            metadataListHeader.Length = Convert.ToUInt16(Records.Count);

            await SerializeEntryMetadataListHeader(stream, metadataListHeader);

            int blockOffset = 0;
            foreach (DatabaseRecord record in Records)
            {
                FileRecordMetadata metadata = new FileRecordMetadata();
                metadata.Attributes = record.Attributes;
                metadata.UniqueId = record.UniqueId;
                metadata.LocalChunkId = (uint)(recordBlockOffset + blockOffset);

                await SerializeRecordMetadata(stream, metadata);

                blockOffset += record.Data.Length;
            }

            // Write padding
            await stream.WriteAsync(new byte[] { 0x00, 0x00 });

            // Write AppInfo
            if (AppInfo != null && AppInfo.Length > 0)
            {
                await stream.WriteAsync(AppInfo);
            }

            // Write SortInfo
            if (SortInfo != null && SortInfo.Length > 0)
            {
                await stream.WriteAsync(SortInfo);
            }

            // Write records...
            foreach (DatabaseRecord record in Records)
            {
                await stream.WriteAsync(record.Data);
            }
        }

        public async Task Deserialize(Stream stream)
        {
            FileDatabaseHeader fileHeader = await DeserializeHeader(stream);

            Header.Name = fileHeader.Name;
            Header.Attributes = fileHeader.Attributes;
            Header.Version = fileHeader.Version;
            Header.CreationDate = fileHeader.CreationDate;
            Header.ModificationDate = fileHeader.ModificationDate;
            Header.LastBackupDate = fileHeader.LastBackupDate;
            Header.ModificationNumber = fileHeader.ModificationNumber;
            Header.Type = fileHeader.Type;
            Header.Creator = fileHeader.Creator;
            Header.UniqueIdSeed = fileHeader.UniqueIdSeed;

            FileEntryMetadataListHeader metadataListHeader = await DeserializeEntryMetadataListHeader(stream);

            if (metadataListHeader.NextListId != 0)
                throw new Exception("PDB files with multiple record lists aren't supported");

            // Read each metadata entry
            List<FileRecordMetadata> metadataList = new List<FileRecordMetadata>();
            for (ushort i = 0; i < metadataListHeader.Length; i++)
            {
                metadataList.Add(await DeserializeRecordMetadata(stream));
            }

            // Read AppInfo block
            if (fileHeader.AppInfoId != 0)
            {
                uint appInfoLength =
                    fileHeader.SortInfoId != 0 ? fileHeader.SortInfoId - fileHeader.AppInfoId :
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - fileHeader.AppInfoId :
                    (uint)stream.Length - fileHeader.AppInfoId;

                stream.Seek(fileHeader.AppInfoId, SeekOrigin.Begin);
                AppInfo = new byte[appInfoLength];
                await FillBuffer(stream, AppInfo);
            }
            
            // Read SortInfo block
            if (fileHeader.SortInfoId != 0)
            {
                uint sortInfoLength =
                    metadataList.Count > 0 ? metadataList.Min(md => md.LocalChunkId) - fileHeader.SortInfoId :
                    (uint)stream.Length - fileHeader.SortInfoId;

                stream.Seek(fileHeader.SortInfoId, SeekOrigin.Begin);
                SortInfo = new byte[sortInfoLength];
                await FillBuffer(stream, SortInfo);
            }

            // Read entries
            Records.Clear();
            foreach (FileRecordMetadata metadata in metadataList)
            {
                FileRecordMetadata? nextMetadata = metadataList
                    .Where(md => md.LocalChunkId > metadata.LocalChunkId)
                    .MinBy(md => md.LocalChunkId);

                uint recordLength =
                    nextMetadata != null ? nextMetadata.LocalChunkId - metadata.LocalChunkId :
                    (uint)stream.Length - metadata.LocalChunkId;

                stream.Seek(metadata.LocalChunkId, SeekOrigin.Begin);
                byte[] recordBuffer = new byte[recordLength];
                await FillBuffer(stream, recordBuffer);

                DatabaseRecord record = new DatabaseRecord();
                record.Attributes = metadata.Attributes;
                record.UniqueId = metadata.UniqueId;
                record.Data = recordBuffer;
                Records.Add(record);
            }
        }

        private static async Task FillBuffer(Stream stream, Memory<byte> buffer)
        {
            int readOffset = 0;
            do
            {
                readOffset += await stream.ReadAsync(buffer.Slice(readOffset));
            } while (readOffset < buffer.Length);
        }

        private static async Task SerializeHeader(Stream stream, FileDatabaseHeader header)
        {
            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            header.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileDatabaseHeader> DeserializeHeader(Stream stream)
        {
            byte[] buffer = new byte[FileDatabaseHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileDatabaseHeader fileHeader = new FileDatabaseHeader();
            fileHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileHeader;
        }

        private static async Task SerializeEntryMetadataListHeader(Stream stream, FileEntryMetadataListHeader header)
        {
            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            header.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileEntryMetadataListHeader> DeserializeEntryMetadataListHeader(Stream stream)
        {
            byte[] buffer = new byte[FileEntryMetadataListHeader.SerializedLength];
            await FillBuffer(stream, buffer);

            FileEntryMetadataListHeader fileEntryMetadataListHeader = new FileEntryMetadataListHeader();
            fileEntryMetadataListHeader.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileEntryMetadataListHeader;
        }

        private static async Task SerializeRecordMetadata(Stream stream, FileRecordMetadata metadata)
        {
            byte[] buffer = new byte[FileRecordMetadata.SerializedLength];
            metadata.Serialize(buffer);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task<FileRecordMetadata> DeserializeRecordMetadata(Stream stream)
        {
            byte[] buffer = new byte[FileRecordMetadata.SerializedLength];
            await FillBuffer(stream, buffer);

            FileRecordMetadata fileRecordMetadata = new FileRecordMetadata();
            fileRecordMetadata.Deserialize(new ReadOnlySequence<byte>(buffer));

            return fileRecordMetadata;
        }
    }
}
