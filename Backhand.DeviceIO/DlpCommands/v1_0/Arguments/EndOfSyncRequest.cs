using System;
using Backhand.DeviceIO.Dlp;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class EndOfSyncRequest : DlpArgument
    {
        public enum EndOfSyncStatus : ushort
        {
            Okay = 0x00,
            OutOfMemoryError = 0x01,
            UseCancelledError = 0x02,
            UnknownError = 0x03,
        }

        public EndOfSyncStatus Status { get; init; }

        public override int GetSerializedLength() =>
            sizeof(ushort);                         // EndOfSyncStatus

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), (ushort)Status);
            offset += sizeof(ushort);

            return offset;
        }
    }
}
