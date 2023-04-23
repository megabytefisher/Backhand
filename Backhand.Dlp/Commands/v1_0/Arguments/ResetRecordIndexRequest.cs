﻿using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ResetRecordIndexRequest : IBinarySerializable
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }
    }
}
