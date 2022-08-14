using Backhand.DeviceIO.Dlp;
using System;
using System.Buffers.Binary;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadSysInfoRequest : DlpArgument
    {
        public ushort HostDlpVersionMajor { get; init; }
        public ushort HostDlpVersionMinor { get; init; }

        public override int GetSerializedLength() =>
            sizeof(ushort) +                        // HostDlpVersionMajor
            sizeof(ushort);                         // HostDlpVersionMinor

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), HostDlpVersionMajor);
            offset += sizeof(ushort);
            
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, sizeof(ushort)), HostDlpVersionMinor);
            offset += sizeof(ushort);

            return offset;
        }
    }
}
