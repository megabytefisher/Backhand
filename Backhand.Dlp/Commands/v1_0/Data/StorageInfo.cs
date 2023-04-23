using System;
using Backhand.Common.BinarySerialization;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0.Data
{
    [GenerateBinarySerialization(MinimumLengthProperty = nameof(ElementLength))]
    public partial class StorageInfo : IBinarySerializable
    {
        [BinarySerialize]
        private byte ElementLength { get; set; }

        [BinarySerialize]
        public byte CardNo { get; set; }

        [BinarySerialize]
        public ushort CardVersion { get; set; }

        [BinarySerialize]
        public DlpDateTime CardDlpDate { get; } = new();

        [BinarySerialize]
        public uint RomSize { get; set; }

        [BinarySerialize]
        public uint RamSize { get; set; }

        [BinarySerialize]
        public uint FreeRam { get; set; }

        [BinarySerialize]
        public byte CardNameSize
        {
            get => (byte)CardNameString.Size;
            set => CardNameString.Size = value;
        }

        [BinarySerialize]
        public byte ManufacturerNameSize
        {
            get => (byte)ManufacturerNameString.Size;
            set => ManufacturerNameString.Size = value;
        }

        [BinarySerialize]
        private FixedSizeBinaryString CardNameString { get; } = new();

        [BinarySerialize]
        private FixedSizeBinaryString ManufacturerNameString { get; } = new();

        public DateTime CardDate
        {
            get => CardDlpDate.AsDateTime;
            set => CardDlpDate.AsDateTime = value;
        }

        public string CardName
        {
            get => CardNameString.Value;
            set => CardNameString.Value = value;
        }

        public string ManufacturerName
        {
            get => ManufacturerNameString.Value;
            set => ManufacturerNameString.Value = value;
        }
    }
}