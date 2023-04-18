using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Arguments
{
    [BinarySerializable]
    public class WriteSysDateTimeRequest : DlpArgument
    {
        [BinarySerialize]
        private DlpDateTime DlpDateTime { get; set; } = new();

        public DateTime DateTime
        {
            get => this.DlpDateTime.AsDateTime;
            set => this.DlpDateTime.AsDateTime = value;
        }
    }
}