using System;
using Backhand.Common.BinarySerialization;
using Backhand.Common.BinarySerialization.Generation;

namespace Backhand.PalmDb.FileIO.FileSerialization
{
    [GenerateBinarySerialization]
    public partial class PdbRecordMetadata : IBinarySerializable
    {
        [BinarySerialize]
        public uint LocalChunkId { get; set; }

        [BinarySerialize]
        public byte RawAttributes { get; set; }

        [BinarySerialize]
        public byte[] UniqueIdBytes { get; } = new byte[3];

        public uint Id
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

        public PdbRecordMetadata()
        {
        }

        public PdbRecordMetadata(PalmDbRecordHeader sourceHeader, uint localChunkId)
        {
            LocalChunkId = localChunkId;
            Attributes = sourceHeader.Attributes;
            Category = sourceHeader.Category;
            Archive = sourceHeader.Archive;
            Id = sourceHeader.Id;
        }
        
        public PalmDbRecordHeader ToPalmDbRecordHeader()
        {
            return new PalmDbRecordHeader
            {
                Attributes = Attributes,
                Category = Category,
                Archive = Archive,
                Id = Id
            };
        }
    }
}
