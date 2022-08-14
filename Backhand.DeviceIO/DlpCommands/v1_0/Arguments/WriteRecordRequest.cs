using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.DlpCommands.v1_0.Data;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class WriteRecordRequest : DlpArgument
    {
        public byte DbHandle { get; set; }
        public byte Flags { get; set; } = 0x80;
        public uint RecordId { get; set; }
        public DlpRecordAttributes Attributes { get; set; }
        public byte Category { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // DbHandle
            sizeof(byte) +                          // Flags
            sizeof(uint) +                          // RecordId
            sizeof(byte) +                          // Attributes
            sizeof(byte) +                          // Category
            (sizeof(byte) * Data.Length);           // Data

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += sizeof(byte);

            buffer[offset] = Flags;
            offset += sizeof(byte);

            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), RecordId);
            offset += sizeof(uint);

            buffer[offset] = (byte)Attributes;
            offset += sizeof(byte);

            buffer[offset] = Category;
            offset += sizeof(byte);

            ((Span<byte>)Data).CopyTo(buffer.Slice(offset));
            offset += sizeof(byte) * Data.Length;

            return offset;
        }
    }
}
