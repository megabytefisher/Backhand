using Backhand.Common.Buffers;
using System;
using System.Buffers;

namespace Backhand.Pdb
{
    public class RawDatabaseRecord : DatabaseRecord
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public override int GetDataSize()
        {
            return Data.Length;
        }

        public override void ReadData(ref SequenceReader<byte> reader)
        {
            Data = new byte[reader.Remaining];
            reader.Sequence.Slice(reader.Consumed, Data.Length).CopyTo(Data);
            reader.Advance(Data.Length);
        }

        public override void WriteData(ref SpanWriter<byte> writer)
        {
            writer.Write(Data);
        }
    }
}
