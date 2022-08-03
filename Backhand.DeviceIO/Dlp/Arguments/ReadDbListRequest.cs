using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class ReadDbListRequest : DlpArgument
    {
        public enum ReadDbListMode : byte
        {
            ListRam         = 0x80,
            ListRom         = 0x40,
            ListMultiple    = 0x20,
        }

        public ReadDbListMode Mode { get; set; }
        public byte CardId { get; set; }
        public ushort StartIndex { get; set; }

        public override int GetSerializedLength()
        {
            return 4;
        }

        public override int Serialize(Span<byte> buffer)
        {
            int offset = 0;
            buffer[offset++] = (byte)Mode;
            buffer[offset++] = CardId;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), StartIndex);
            offset += 2;

            return offset;
        }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
