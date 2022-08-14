using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadSysInfoDlpVersionsResponse : DlpArgument
    {
        public ushort ClientDlpVersionMajor { get; private set; }
        public ushort ClientDlpVersionMinor { get; private set; }
        public ushort MinimumDlpVersionMajor { get; private set; }
        public ushort MinimumDlpVersionMinor { get; private set; }
        public uint MaxRecordSize { get; private set; }

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            ClientDlpVersionMajor = bufferReader.ReadUInt16BigEndian();
            ClientDlpVersionMinor = bufferReader.ReadUInt16BigEndian();
            MinimumDlpVersionMajor = bufferReader.ReadUInt16BigEndian();
            MinimumDlpVersionMinor = bufferReader.ReadUInt16BigEndian();
            MaxRecordSize = bufferReader.ReadUInt32BigEndian();
        }
    }
}
