using Backhand.Common.Buffers;
using Backhand.Pdb.FileSerialization;
using System;
using System.Buffers;

namespace Backhand.Pdb
{
    public abstract class DatabaseRecord
    {
        public DatabaseRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public bool Archive { get; set; }
        public uint UniqueId { get; set; }

        public abstract int GetDataSize();
        public abstract void WriteData(ref SpanWriter<byte> writer);
        public abstract void ReadData(ref SequenceReader<byte> reader);

        public void WriteData(Span<byte> buffer)
        {
            var writer = new SpanWriter<byte>(buffer);
            WriteData(ref writer);
        }

        public void ReadData(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            ReadData(ref reader);
        }

        internal void WriteFileMetadata(PdbRecordMetadata header, uint localChunkId)
        {
            header.Attributes = Attributes;
            header.Category = Category;
            header.Archive = Archive;
            header.UniqueId = UniqueId;
            header.LocalChunkId = localChunkId;
        }

        internal void ReadFileMetadata(PdbRecordMetadata header)
        {
            Attributes = header.Attributes;
            Category = header.Category;
            Archive = header.Archive;
            UniqueId = header.UniqueId;
        }
    }
}
