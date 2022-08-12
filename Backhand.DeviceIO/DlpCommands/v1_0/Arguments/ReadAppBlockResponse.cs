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
    public class ReadAppBlockResponse : DlpArgument
    {
        public ushort Size { get; set; }
        public byte[] Data { get; set; }

        public override int GetSerializedLength()
        {
            throw new NotImplementedException();
        }

        public override int Serialize(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        public void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            Size = bufferReader.ReadUInt16BigEndian();

            Data = new byte[bufferReader.Remaining];
            bufferReader.Sequence.Slice(bufferReader.Position).CopyTo(Data);
        }
    }
}
