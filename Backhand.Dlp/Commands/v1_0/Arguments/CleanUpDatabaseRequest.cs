using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;
using System;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class CleanUpDatabaseRequest : DlpArgument
    {
        [BinarySerialize]
        public byte DbHandle { get; set; }
    }
}
