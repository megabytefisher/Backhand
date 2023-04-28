namespace Backhand.Common.BinarySerialization.Generation
{
    internal static class Names
    {
        public static class Types
        {
            public const string Byte = "byte";
            public const string Bool = "bool";
            public const string UInt16 = "ushort";
            public const string Int16 = "short";
            public const string UInt32 = "uint";
            public const string Int32 = "int";
            public const string UInt64 = "ulong";
            public const string Int64 = "long";

            public static readonly string GenerateBinarySerializationAttribute =
                typeof(GenerateBinarySerializationAttribute).FullName;
            public static readonly string BinarySerializeAttribute =
                typeof(BinarySerializeAttribute).FullName;
            
            public const string ByteSequenceReader = "System.Buffers.SequenceReader<byte>";
            public const string ByteSpanWriter = "Backhand.Common.Buffers.SpanWriter<byte>";

            public const string IBinarySerializable = "Backhand.Common.BinarySerialization.IBinarySerializable";
        }

        public static class Properties
        {
            public static class Array
            {
                public const string Length = nameof(Length);
            }

            public static class SequenceReader
            {
                public const string Consumed = nameof(Consumed);
            }

            public static class SpanWriter
            {
                public const string Index = nameof(Index);
            }
        }

        public static class Methods
        {
            public static class IEnumerable
            {
                public const string Sum = nameof(Sum);
            }

            public static class SequenceReader
            {
                public const string Advance = nameof(Advance);

                public const string Read = nameof(Read);
                public const string ReadUInt16LittleEndian = nameof(ReadUInt16LittleEndian);
                public const string ReadUInt16BigEndian = nameof(ReadUInt16BigEndian);
                public const string ReadInt16LittleEndian = nameof(ReadInt16LittleEndian);
                public const string ReadInt16BigEndian = nameof(ReadInt16BigEndian);
                public const string ReadUInt32LittleEndian = nameof(ReadUInt32LittleEndian);
                public const string ReadUInt32BigEndian = nameof(ReadUInt32BigEndian);
                public const string ReadInt32LittleEndian = nameof(ReadInt32LittleEndian);
                public const string ReadInt32BigEndian = nameof(ReadInt32BigEndian);
                public const string ReadUInt64LittleEndian = nameof(ReadUInt64LittleEndian);
                public const string ReadUInt64BigEndian = nameof(ReadUInt64BigEndian);
                public const string ReadInt64LittleEndian = nameof(ReadInt64LittleEndian);
                public const string ReadInt64BigEndian = nameof(ReadInt64BigEndian);
            }

            public static class SpanWriter
            {
                public const string Advance = nameof(Advance);

                public const string Write = nameof(Write);
                public const string WriteUInt16LittleEndian = nameof(WriteUInt16LittleEndian);
                public const string WriteUInt16BigEndian = nameof(WriteUInt16BigEndian);
                public const string WriteInt16LittleEndian = nameof(WriteInt16LittleEndian);
                public const string WriteInt16BigEndian = nameof(WriteInt16BigEndian);
                public const string WriteUInt32LittleEndian = nameof(WriteUInt32LittleEndian);
                public const string WriteUInt32BigEndian = nameof(WriteUInt32BigEndian);
                public const string WriteInt32LittleEndian = nameof(WriteInt32LittleEndian);
                public const string WriteInt32BigEndian = nameof(WriteInt32BigEndian);
                public const string WriteUInt64LittleEndian = nameof(WriteUInt64LittleEndian);
                public const string WriteUInt64BigEndian = nameof(WriteUInt64BigEndian);
                public const string WriteInt64LittleEndian = nameof(WriteInt64LittleEndian);
                public const string WriteInt64BigEndian = nameof(WriteInt64BigEndian);
            }

            public static class IBinarySerializable
            {
                public const string GetSize = nameof(GetSize);
                public const string Read = nameof(Read);
                public const string Write = nameof(Write);
            }
        }
    }
}
