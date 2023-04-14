using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class EndSyncRequest : DlpArgument
    {
        public enum EndOfSyncStatus : ushort
        {
            Okay = 0x00,
            OutOfMemoryError = 0x01,
            UserCancelledError = 0x02,
            UnknownError = 0x03,
        }

        [BinarySerialize]
        public EndOfSyncStatus Status { get; set; }
    }
}
