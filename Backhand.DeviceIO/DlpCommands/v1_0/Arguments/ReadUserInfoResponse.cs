using Backhand.DeviceIO.Dlp;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Text;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Arguments
{
    public class ReadUserInfoResponse : DlpArgument
    {
        public uint UserId { get; private set; }
        public uint ViewerId { get; private set; }
        public uint LastSyncPcId { get; private set; }
        public DateTime LastSuccessfulSyncDate { get; private set; }
        public DateTime LastSyncDate { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public byte[] Password { get; private set; } = Array.Empty<byte>();

        public override SequencePosition Deserialize(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);
            Deserialize(ref bufferReader);
            return bufferReader.Position;
        }

        private void Deserialize(ref SequenceReader<byte> bufferReader)
        {
            UserId = bufferReader.ReadUInt32BigEndian();
            ViewerId = bufferReader.ReadUInt32BigEndian();
            LastSyncPcId = bufferReader.ReadUInt32BigEndian();
            LastSuccessfulSyncDate = ReadDlpDateTime(ref bufferReader);
            LastSyncDate = ReadDlpDateTime(ref bufferReader);
            byte usernameLength = bufferReader.Read();
            byte passwordLength = bufferReader.Read();

            if (usernameLength > 0)
            {
                Username = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, usernameLength - 1));
                bufferReader.Advance(usernameLength);
            }
            else
            {
                Username = string.Empty;
            }

            if (passwordLength > 0)
            {
                Password = new byte[passwordLength];
                bufferReader.Sequence.Slice(bufferReader.Position, passwordLength).CopyTo(Password);
                bufferReader.Advance(passwordLength);
            }
        }
    }
}
