using Backhand.Common.Buffers;
using System.Buffers;
using System.Text;

namespace Backhand.Pdb.Stream
{
    public class FileStreamRecord : DatabaseRecord
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();

        private static readonly byte[] HeaderMagicBytes = Encoding.ASCII.GetBytes("DBLK");

        public override int GetDataSize()
        {
            return
                HeaderMagicBytes.Length +
                sizeof(uint) + // Length of content
                Content.Length;
        }

        public override void ReadData(ref SequenceReader<byte> reader)
        {
            foreach (byte headerByte in HeaderMagicBytes)
            {
                if (reader.Read() != headerByte)
                {
                    throw new InvalidDataException("Invalid header magic bytes");
                }
            }

            uint contentLength = reader.ReadUInt32BigEndian();
            Content = new byte[contentLength];
            reader.ReadExact(Convert.ToInt32(contentLength)).CopyTo(Content);
        }

        public override void WriteData(ref SpanWriter<byte> writer)
        {
            writer.Write(HeaderMagicBytes);
            writer.WriteUInt32BigEndian(Convert.ToUInt32(Content.Length));
            writer.Write(Content);
        }
    }
}
