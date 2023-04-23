using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadRecordIdListResponse : IBinarySerializable
    {
        [BinarySerialize]
        public ushort Count
        {
            get => (ushort)RecordIds.Length;
            set => RecordIds = new uint[value];
        }

        [BinarySerialize]
        public uint[] RecordIds { get; private set; } = Array.Empty<uint>();
    }
}