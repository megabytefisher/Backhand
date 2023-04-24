using Backhand.Common.BinarySerialization;
using System;
using System.Linq;
using Backhand.Common.BinarySerialization.Generation;
using Backhand.Dlp.Commands.v1_0.Data;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadDbListResponse : IBinarySerializable
    {
        [BinarySerialize]
        public ushort LastIndex { get; set; }

        [BinarySerialize]
        public byte Flags { get; set; }

        [BinarySerialize]
        public byte ResultCount
        {
            get => (byte)Results.Length;
            set => Results = Enumerable.Range(1, value).Select(_ => new DatabaseMetadata()).ToArray();
        }

        [BinarySerialize]
        public DatabaseMetadata[] Results { get; private set; } = Array.Empty<DatabaseMetadata>();
    }
}
