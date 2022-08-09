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
    public class ReadSysInfoResponse : DlpArgument
    {
        public uint RomVersion { get; set; }
        public uint Locale { get; set; }
        public byte[] ProductId { get; set; } = Array.Empty<byte>();

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
            RomVersion = bufferReader.ReadUInt32BigEndian();
            Locale = bufferReader.ReadUInt32BigEndian();

            byte productIdLength = bufferReader.Read();
            ProductId = new byte[productIdLength];
            bufferReader.Sequence.Slice(bufferReader.Position, productIdLength).CopyTo(ProductId);
        }
    }
}
