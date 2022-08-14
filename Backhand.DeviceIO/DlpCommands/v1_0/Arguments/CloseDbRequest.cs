using System;
using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class CloseDbRequest : DlpArgument
    {
        public byte DbHandle { get; init; }

        public override int GetSerializedLength() =>
            sizeof(byte);                           // DbHandle

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += sizeof(byte);

            return offset;
        }
    }
}
