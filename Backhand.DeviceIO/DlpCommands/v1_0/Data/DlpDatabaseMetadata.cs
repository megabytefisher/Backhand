using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    public class DlpDatabaseMetadata : DlpArgument
    {
        public byte MiscFlags { get; set; }
        public DlpDatabaseAttributes Attributes { get; set; }
        public string Type { get; set; } = "";
        public string Creator { get; set; } = "";
        public ushort Version { get; set; }
        public uint ModificationNumber { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public ushort Index { get; set; }
        public string Name { get; set; } = "";

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
            byte length = bufferReader.Read();

            ReadOnlySequence<byte> metadataBuffer = bufferReader.Sequence.Slice(bufferReader.Position, length - 1);
            SequenceReader<byte> metadataReader = new SequenceReader<byte>(metadataBuffer);

            MiscFlags = metadataReader.Read();
            Attributes = (DlpDatabaseAttributes)metadataReader.ReadUInt16BigEndian();

            Type = Encoding.ASCII.GetString(metadataReader.Sequence.Slice(metadataReader.Position, 4));
            metadataReader.Advance(4);

            Creator = Encoding.ASCII.GetString(metadataReader.Sequence.Slice(metadataReader.Position, 4));
            metadataReader.Advance(4);

            Version = metadataReader.ReadUInt16BigEndian();
            ModificationNumber = metadataReader.ReadUInt32BigEndian();
            CreationDate = ReadDlpDateTime(ref metadataReader);
            ModificationDate = ReadDlpDateTime(ref metadataReader);
            LastBackupDate = ReadDlpDateTime(ref metadataReader);
            Index = metadataReader.ReadUInt16BigEndian();
            Name = ReadNullTerminatedString(ref metadataReader);

            bufferReader.Advance(length - 1);
        }
    }
}
