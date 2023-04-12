using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerialized]
    public class EndOfSyncRequest : DlpArgument
    {
        public enum EndOfSyncStatus : ushort
        {
            Okay = 0x00,
            OutOfMemoryError = 0x01,
            UserCancelledError = 0x02,
            UnknownError = 0x03,
        }

        [BinarySerialized]
        public EndOfSyncStatus Status { get; set; }
    }
}
