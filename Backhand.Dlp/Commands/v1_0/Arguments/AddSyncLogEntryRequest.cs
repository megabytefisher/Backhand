using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class AddSyncLogEntryRequest : IBinarySerializable
    {
        [BinarySerialize] private NullTerminatedBinaryString MessageString { get; } = new();

        public string Message
        {
            get => MessageString;
            set => MessageString.Value = value;
        }
    }
}