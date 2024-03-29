﻿using Backhand.Common.BinarySerialization;
using System;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadDbListRequest : IBinarySerializable
    {
        [BinarySerialize] public ReadDbListMode Mode { get; set; }
        [BinarySerialize] public byte CardId { get; set; }
        [BinarySerialize] public ushort StartIndex { get; set; }
        
        [Flags]
        public enum ReadDbListMode : byte
        {
            None            = 0b00000000,
            ListRam         = 0b10000000,
            ListRom         = 0b01000000,
        }
    }
}
