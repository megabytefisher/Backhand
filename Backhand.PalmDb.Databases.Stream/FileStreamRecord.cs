using Backhand.Common.Buffers;
using System.Buffers;
using Backhand.Common.BinarySerialization;

namespace Backhand.PalmDb.Databases.Stream;

public class FileStreamRecord : IBinarySerializable
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    private static readonly byte[] HeaderMagicBytes = "DBLK"u8.ToArray();

    public int GetSize()
    {
        return
            HeaderMagicBytes.Length +
            sizeof(uint) + // Length of content
            Content.Length;
    }

    public void Read(ref SequenceReader<byte> reader)
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

    public void Write(ref SpanWriter<byte> writer)
    {
        writer.Write(HeaderMagicBytes);
        writer.WriteUInt32BigEndian(Convert.ToUInt32(Content.Length));
        writer.Write(Content);
    }
}