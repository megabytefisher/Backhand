using System;
using System.Buffers.Binary;
using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class WriteSortBlockRequest : DlpArgument
    {
        public byte DbHandle { get; init; }
        public byte[] Data { get; init; } = Array.Empty<byte>();

        public override int GetSerializedLength() =>
            sizeof(byte) +                          // DbHandle
            sizeof(byte) +                          // (Padding)
            sizeof(ushort) +                        // (Data Length)
            (sizeof(byte) * Data.Length);           // Data

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += sizeof(byte);

            buffer[offset] = 0; // Padding
            offset += sizeof(byte);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), Convert.ToUInt16(Data.Length));
            offset += sizeof(ushort);

            Data.CopyTo(buffer.Slice(offset));
            offset += sizeof(byte) * Data.Length;

            return offset;
        }
    }
}
