using System;
using System.Linq;
using Backhand.Common.BinarySerialization;
using Backhand.Dlp.Commands.v1_0.Data;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [GenerateBinarySerialization]
    public partial class ReadStorageInfoMainResponse : IBinarySerializable
    {
        [BinarySerialize]
        public byte LastCard { get; set; }

        [BinarySerialize]
        public byte More { get; set; }

        [BinarySerialize]
        private byte Padding { get; set; } = 0;

        [BinarySerialize]
        public byte ResultCount
        {
            get => (byte)Results.Length;
            set => Results = Enumerable.Range(0, value).Select(_ => new StorageInfo()).ToArray();
        }

        [BinarySerialize]
        public StorageInfo[] Results { get; set; } = Array.Empty<StorageInfo>();
    }
}
