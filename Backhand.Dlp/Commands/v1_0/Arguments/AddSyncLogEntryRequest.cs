using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class AddSyncLogEntryRequest : DlpArgument
    {
        [BinarySerialize]
        private NullTerminatedBinaryString MessageString { get; } = new();

        public string Message
        {
            get => MessageString.Value;
            set => MessageString.Value = value;
        }
    }
}
