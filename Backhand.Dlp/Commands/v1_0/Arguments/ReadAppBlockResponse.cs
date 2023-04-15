using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using Backhand.Protocols.Dlp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class ReadAppBlockResponse : DlpArgument
    {
        [BinarySerialize]
        public ushort Size
        {
            get => (byte)Data.Length;
            set => Data = new byte[value];
        }

        [BinarySerialize]
        public byte[] Data { get; private set; } = Array.Empty<byte>();
    }
}
