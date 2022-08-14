using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadSysInfoResponse : DlpArgument
    {
        public uint RomVersion { get; private set; }
        public uint Locale { get; private set; }
        public byte[] ProductId { get; private set; } = Array.Empty<byte>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            RomVersion = bufferReader.ReadUInt32BigEndian();
            Locale = bufferReader.ReadUInt32BigEndian();

            byte productIdLength = bufferReader.Read();
            ProductId = new byte[productIdLength];
            bufferReader.Sequence.Slice(bufferReader.Position, productIdLength).CopyTo(ProductId);
        }
    }
}
