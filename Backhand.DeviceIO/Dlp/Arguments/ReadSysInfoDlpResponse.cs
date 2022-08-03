using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class ReadSysInfoDlpResponse : DlpArgument
    {
        public ushort ClientDlpVersionMajor { get; set; }
        public ushort ClientDlpVersionMinor { get; set; }
        public ushort MinimumDlpVersionMajor { get; set; }
        public ushort MinimumDlpVersionMinor { get; set; }
        public uint MaxRecordSize { get; set; }

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
            ClientDlpVersionMajor = bufferReader.ReadUInt16BigEndian();
            ClientDlpVersionMinor = bufferReader.ReadUInt16BigEndian();
            MinimumDlpVersionMajor = bufferReader.ReadUInt16BigEndian();
            MinimumDlpVersionMinor = bufferReader.ReadUInt16BigEndian();
            MaxRecordSize = bufferReader.ReadUInt32BigEndian();
        }
    }
}
