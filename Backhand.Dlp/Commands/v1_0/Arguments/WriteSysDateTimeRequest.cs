using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class WriteSysDateTimeRequest : IBinarySerializable
    {
        [BinarySerialize] private DlpDateTime DlpDateTime { get; set; } = new();

        public DateTime DateTime
        {
            get => DlpDateTime;
            set => DlpDateTime.AsDateTime = value;
        }
    }
}