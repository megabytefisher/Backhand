using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Slp
{
    public class SlpPacket
    {
        public byte DestinationSocket { get; set; }
        public byte SourceSocket { get; set; }
        public byte PacketType { get; set; }
        public byte TransactionId { get; set; }
        public ReadOnlySequence<byte> Data { get; set; }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Dest: {DestinationSocket}, Src: {SourceSocket}, Type: {PacketType}, TxId: {TransactionId}, Data Length: {Data.Length}");
            result.Append(HexSerialization.GetHexString(Data));
            return result.ToString();
        }
    }
}
