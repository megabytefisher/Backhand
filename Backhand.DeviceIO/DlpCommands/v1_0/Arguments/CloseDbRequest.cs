using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class CloseDbRequest : DlpArgument
    {
        public byte DbHandle { get; set; }

        public override int GetSerializedLength()
        {
            return sizeof(byte);
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;

            buffer[offset] = DbHandle;
            offset += 1;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            DbHandle = bufferReader.Read();
        }
    }
}
