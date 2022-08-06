using Backhand.DeviceIO.Dlp;
using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadUserInfoResponse : DlpArgument
    {
        public uint UserId { get; set; }
        public uint ViewerId { get; set; }
        public uint LastSyncPcId { get; set; }
        public DateTime LastSuccessfulSyncDate { get; set; }
        public DateTime LastSyncDate { get; set; }
        public string Username { get; set; } = string.Empty;
        public byte[] Password { get; set; } = Array.Empty<byte>();

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

            UserId = bufferReader.ReadUInt32BigEndian();
            ViewerId = bufferReader.ReadUInt32BigEndian();
            LastSyncPcId = bufferReader.ReadUInt32BigEndian();
            LastSuccessfulSyncDate = ReadDlpDateTime(ref bufferReader);
            LastSyncDate = ReadDlpDateTime(ref bufferReader);
            byte usernameLength = bufferReader.Read();
            byte passwordLength = bufferReader.Read();

            Username = Encoding.ASCII.GetString(buffer.Slice(bufferReader.Position, usernameLength - 1));
            bufferReader.Advance(usernameLength);

            Password = new byte[passwordLength];
            buffer.Slice(bufferReader.Position, passwordLength).CopyTo(Password);
            bufferReader.Advance(passwordLength);

            return bufferReader.Position;
        }
    }
}
