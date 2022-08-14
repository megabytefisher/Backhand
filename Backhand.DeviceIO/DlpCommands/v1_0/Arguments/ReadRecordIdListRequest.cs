using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadRecordIdListRequest : DlpArgument
    {
        [Flags]
        public enum ReadRecordIdListFlags : byte
        {
            ShouldSort          = 0b10000000,
        }

        public byte DbHandle { get; init; }
        public ReadRecordIdListFlags Flags { get; init; }
        public ushort StartIndex { get; init; }
        public ushort MaxRecords { get; init; }

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // DbHandle
            sizeof(byte) +                          // Flags
            sizeof(ushort) +                        // StartIndex
            sizeof(ushort);                         // MaxRecords

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            
            buffer[offset] = DbHandle;
            offset += sizeof(byte);
            
            buffer[offset] = (byte)Flags;
            offset += sizeof(byte);
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), StartIndex);
            offset += sizeof(ushort);
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), MaxRecords);
            offset += sizeof(ushort);

            return offset;
        }
    }
}
