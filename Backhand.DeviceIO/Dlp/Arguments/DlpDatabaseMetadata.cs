using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp.Arguments
{
    public class DlpDatabaseMetadata : DlpArgument
    {
        [Flags]
        public enum DatabaseFlags : ushort
        {
            Open                = 0b10000000_00000000,
            Unused1             = 0b01000000_00000000,
            Unused2             = 0b00100000_00000000,
            Unused3             = 0b00010000_00000000,
            Bundle              = 0b00001000_00000000,
            Recyclable          = 0b00000100_00000000,
            LaunchableData      = 0b00000010_00000000,
            Hidden              = 0b00000001_00000000,
            Stream              = 0b00000000_10000000,
            CopyPrevention      = 0b00000000_01000000,
            ResetAfterInstall   = 0b00000000_00100000,
            OkToInstallNewer    = 0b00000000_00010000,
            Backup              = 0b00000000_00001000,
            AppInfoDirty        = 0b00000000_00000100,
            ReadOnly            = 0b00000000_00000010,
            ResourceDb          = 0b00000000_00000001,
        }

        public byte MiscFlags { get; set; }
        public DatabaseFlags Flags { get; set; }
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
            MiscFlags = bufferReader.Read();
            Flags = (DatabaseFlags)bufferReader.ReadUInt16BigEndian();

            Type = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, 4));
            bufferReader.Advance(4);

            Creator = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, 4));
            bufferReader.Advance(4);

            Version = bufferReader.ReadUInt16BigEndian();
            ModificationNumber = bufferReader.ReadUInt32BigEndian();
            CreationDate = ReadDlpDateTime(ref bufferReader);
            ModificationDate = ReadDlpDateTime(ref bufferReader);
            LastBackupDate = ReadDlpDateTime(ref bufferReader);
            Index = bufferReader.ReadUInt16BigEndian();
            Name = ReadNullTerminatedString(ref bufferReader);
        }
    }
}
