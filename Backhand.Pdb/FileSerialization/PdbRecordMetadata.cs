using System;
using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [GenerateBinarySerialization]
    internal partial class PdbRecordMetadata : IBinarySerializable
    {
        [BinarySerialize]
        public uint LocalChunkId { get; set; }

        [BinarySerialize]
        public byte RawAttributes { get; set; }

        [BinarySerialize]
        public byte[] UniqueIdBytes { get; set; } = new byte[3];

        public uint UniqueId
        {
            get => (uint)(UniqueIdBytes[0] << 16 | UniqueIdBytes[1] << 8 | UniqueIdBytes[2]);
            set
            {
                UniqueIdBytes[0] = (byte)(value >> 16);
                UniqueIdBytes[1] = (byte)(value >> 8);
                UniqueIdBytes[2] = (byte)value;
            }
        }

        public byte Category
        {
            get => (byte)(RawAttributes & 0b1111);
            set => RawAttributes = (byte)((RawAttributes & 0b11110000) | (value & 0b1111));
        }

        public bool Archive
        {
            get => Attributes.HasFlag(DatabaseRecordAttributes.Delete | DatabaseRecordAttributes.Dirty) && (RawAttributes & 0b1000) != 0;
            set
            {
                if (!Attributes.HasFlag(DatabaseRecordAttributes.Delete | DatabaseRecordAttributes.Dirty))
                {
                    if (!value)
                        return;

                    throw new InvalidOperationException("Archive can only be set if the record is deleted or dirty.");
                }

                RawAttributes = (byte)((RawAttributes & 0b11101111) | (value ? 0b1000 : 0));
            }
        }

        public DatabaseRecordAttributes Attributes
        {
            get => (DatabaseRecordAttributes)RawAttributes;
            set => RawAttributes = (byte)value;
        }
    }
}
