using Backhand.Common.BinarySerialization.Internal;
using Backhand.Common.Buffers;
using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using SequenceReaderExtensions = Backhand.Common.Buffers.SequenceReaderExtensions;

namespace Backhand.Common.BinarySerialization
{
    public static class BinarySerializer<T>
    {
        private delegate int GetSizeImplementation(T value);
        private delegate void SerializeImplementation(T value, ref SpanWriter<byte> bufferWriter);
        private delegate void DeserializeImplementation(ref SequenceReader<byte> bufferReader, T value);

        private static readonly Lazy<GetSizeImplementation> _getSize = new(BuildGetSize);
        private static readonly Lazy<SerializeImplementation> _serialize = new(BuildSerialize);
        private static readonly Lazy<DeserializeImplementation> _deserialize = new(BuildDeserialize);

        public static int GetSize(T value) => _getSize.Value(value);
        public static void Serialize(T value, ref SpanWriter<byte> buffer) => _serialize.Value(value, ref buffer);
        public static void Deserialize(ref SequenceReader<byte> buffer, T value) => _deserialize.Value(ref buffer, value);

        private static GetSizeImplementation BuildGetSize()
        {
            BinarySerializedAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializedAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The only parameter is a T (value)
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));

            // We need to add code to calculate the size of each BinarySerialized property
            Expression result = Expression.Constant(0);

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                BinarySerializedAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializedAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                result = Expression.Add(result, GetSizeExpression(propertyInfo.PropertyType, propertyOptions, Expression.Property(value, propertyInfo)));
            }

            return Expression.Lambda<GetSizeImplementation>(result, value).Compile();
        }

        private static SerializeImplementation BuildSerialize()
        {
            BinarySerializedAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializedAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The two parameters are a T (value) and a reference to a SpanWriter<byte> (bufferWriter)
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));
            ParameterExpression bufferWriter = Expression.Parameter(typeof(SpanWriter<byte>).MakeByRefType(), nameof(bufferWriter));

            // Local variables
            ParameterExpression loopIterator = Expression.Variable(typeof(int), "loopIterator");

            // List of body expressions
            List<Expression> bodyItems = new List<Expression>();

            // We need to add code to serialize each BinarySerialized property
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                BinarySerializedAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializedAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                bodyItems.Add(GetWriteExpression(propertyInfo.PropertyType, bufferWriter, propertyOptions, Expression.Property(value, propertyInfo)));
            }

            BlockExpression body = Expression.Block(new[] { loopIterator }, bodyItems);

            return Expression.Lambda<SerializeImplementation>(body, value, bufferWriter).Compile();
        }

        private static DeserializeImplementation BuildDeserialize()
        {
            BinarySerializedAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializedAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The two parameters are a SequenceReader<byte> (reader) and a reference to a T (value)
            ParameterExpression bufferReader = Expression.Parameter(typeof(SequenceReader<byte>).MakeByRefType(), nameof(bufferReader));
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));

            // List of body expressions
            List<Expression> bodyItems = new List<Expression>();

            // We need to add code to deserialize each BinarySerialized property
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                BinarySerializedAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializedAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                bodyItems.Add(GetReadExpression(propertyInfo.PropertyType, bufferReader, propertyOptions, Expression.Property(value, propertyInfo)));
            }

            BlockExpression body = Expression.Block(bodyItems);

            return Expression.Lambda<DeserializeImplementation>(body, bufferReader, value).Compile();
        }

        private static Expression GetSizeExpression(Type type, SerializerOptions options, Expression value)
        {
            int? primitiveSize = Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => sizeof(byte),
                TypeCode.UInt16 => sizeof(ushort),
                TypeCode.Int16 => sizeof(short),
                TypeCode.UInt32 => sizeof(uint),
                TypeCode.Int32 => sizeof(int),
                TypeCode.UInt64 => sizeof(ulong),
                TypeCode.Int64 => sizeof(long),
                _ => null
            };

            if (primitiveSize.HasValue)
            {
                return Expression.Constant(primitiveSize.Value);
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType() ?? throw new Exception("Couldn't get array property element type");

                LabelTarget loopEnd = Expression.Label(typeof(int), "loopEnd");
                ParameterExpression i = Expression.Variable(typeof(int), "i");
                ParameterExpression result = Expression.Variable(typeof(int), "result");

                return Expression.Block(
                    new[] { i, result },
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(i, Expression.ArrayLength(value)),
                            Expression.Block(
                                Expression.AddAssign(result, GetSizeExpression(elementType, options, Expression.ArrayAccess(value, i))),
                                Expression.PostIncrementAssign(i)
                            ),
                            Expression.Break(loopEnd, result)
                        ),
                        loopEnd
                    )
                );
            }
            else if (type.GetCustomAttribute<BinarySerializedAttribute>() != null)
            {
                Type propertySerializerType = typeof(BinarySerializer<>).MakeGenericType(type);
                MethodInfo propertySizeMethod = propertySerializerType.GetMethod(nameof(GetSize)) ?? throw new Exception("Couldn't get GetSize method for property type");
                return Expression.Call(null, propertySizeMethod, value);
            }
            else
            {
                throw new Exception("Unknown BinarySerialized property type");
            }
        }

        private static Expression GetWriteExpression(Type writeType, ParameterExpression bufferWriter, SerializerOptions options, Expression value)
        {
            Expression? primitiveWrite = Type.GetTypeCode(writeType) switch
            {
                TypeCode.Byte => WriterMethods.GetWriteExpression(bufferWriter, value),
                TypeCode.UInt16 => options.Endian == Endian.Little ? WriterMethods.GetWriteUInt16LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteUInt16BigEndianExpression(bufferWriter, value),
                TypeCode.Int16 => options.Endian == Endian.Little ? WriterMethods.GetWriteInt16LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteInt16BigEndianExpression(bufferWriter, value),
                TypeCode.UInt32 => options.Endian == Endian.Little ? WriterMethods.GetWriteUInt32LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteUInt32BigEndianExpression(bufferWriter, value),
                TypeCode.Int32 => options.Endian == Endian.Little ? WriterMethods.GetWriteInt32LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteInt32BigEndianExpression(bufferWriter, value),
                TypeCode.UInt64 => options.Endian == Endian.Little ? WriterMethods.GetWriteUInt64LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteUInt64BigEndianExpression(bufferWriter, value),
                TypeCode.Int64 => options.Endian == Endian.Little ? WriterMethods.GetWriteInt64LittleEndianExpression(bufferWriter, value) : WriterMethods.GetWriteInt64BigEndianExpression(bufferWriter, value),
                _ => null
            };

            if (primitiveWrite != null)
            {
                return primitiveWrite;
            }
            else if (writeType.IsArray)
            {
                Type elementType = writeType.GetElementType() ?? throw new Exception("Couldn't get array property element type");
                Expression arrayLength = Expression.ArrayLength(value);

                if (elementType == typeof(byte))
                {
                    return Expression.Block(
                        ArrayMethods.GetCopyToSpanExpression(value, Expression.Property(bufferWriter, nameof(SpanWriter<byte>.RemainingSpan))),
                        WriterMethods.GetAdvanceExpression(bufferWriter, arrayLength)
                    );
                }

                LabelTarget loopEnd = Expression.Label("loopEnd");
                ParameterExpression i = Expression.Parameter(typeof(int), "i");

                return Expression.Block(
                    new[] { i },
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(i, arrayLength),
                            Expression.Block(
                                GetWriteExpression(elementType, bufferWriter, options, Expression.ArrayAccess(value, i)),
                                Expression.PostIncrementAssign(i)
                            ),
                            Expression.Break(loopEnd)
                        ),
                        loopEnd
                    )
                );
            }
            else if (writeType.GetCustomAttribute<BinarySerializedAttribute>() != null)
            {
                Type propertySerializerType = typeof(BinarySerializer<>).MakeGenericType(writeType);
                MethodInfo propertySerializeMethod = propertySerializerType.GetMethod(nameof(Serialize)) ?? throw new Exception("Couldn't get Serialize method for property type");

                return Expression.Call(null, propertySerializeMethod, value, bufferWriter);
            }
            else
            {
                throw new Exception("Unknown BinarySerialized property type");
            }
        }

        private static Expression GetReadExpression(Type readType, ParameterExpression bufferReader, SerializerOptions options, Expression value)
        {
            MethodInfo? primitiveReadMethod = Type.GetTypeCode(readType) switch
            {
                TypeCode.Byte => Read,
                TypeCode.UInt16 => options.Endian == Endian.Little ? ReadUInt16LittleEndian : ReadUInt16BigEndian,
                TypeCode.Int16 => options.Endian == Endian.Little ? ReadInt16LittleEndian : ReadInt16BigEndian,
                TypeCode.UInt32 => options.Endian == Endian.Little ? ReadUInt32LittleEndian : ReadUInt32BigEndian,
                TypeCode.Int32 => options.Endian == Endian.Little ? ReadInt32LittleEndian : ReadInt32BigEndian,
                TypeCode.UInt64 => options.Endian == Endian.Little ? ReadUInt64LittleEndian : ReadUInt64BigEndian,
                TypeCode.Int64 => options.Endian == Endian.Little ? ReadInt64LittleEndian : ReadInt64BigEndian,
                _ => null
            };

            if (primitiveReadMethod != null)
            {
                return Expression.Assign(value, Expression.Call(primitiveReadMethod, bufferReader));
            }
            else if (readType.IsArray)
            {
                Type elementType = readType.GetElementType() ?? throw new Exception("Couldn't get array property element type");

                LabelTarget loopEnd = Expression.Label("loopEnd");
                ParameterExpression i = Expression.Parameter(typeof(int), "i");

                return Expression.Block(
                    new[] { i },
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(i, Expression.ArrayLength(value)),
                            Expression.Block(
                                GetReadExpression(elementType, bufferReader, options, Expression.ArrayAccess(value, i)),
                                Expression.PostIncrementAssign(i)
                            ),
                            Expression.Break(loopEnd)
                        ),
                        loopEnd
                    )
                );
            }
            else if (readType.GetCustomAttribute<BinarySerializedAttribute>() != null)
            {
                Type propertySerializerType = typeof(BinarySerializer<>).MakeGenericType(readType);
                MethodInfo propertyDeserializeMethod = propertySerializerType.GetMethod(nameof(Deserialize)) ?? throw new Exception("Couldn't get Deserialize method for property type");
                return Expression.Call(null, propertyDeserializeMethod, bufferReader, value);
            }
            else
            {
                throw new Exception("Unknown BinarySerialized property type");
            }
        }

        private ref struct SerializerOptions
        {
            public Endian Endian { get; private init; }
            public StringEncoding StringEncoding { get; private init; }
            public string LengthPropertyName { get; private init; }
            public bool NullTerminated { get; private init; }

            public SerializerOptions()
            {
                Endian = Endian.Big;
                StringEncoding = StringEncoding.ASCII;
                LengthPropertyName = string.Empty;
                NullTerminated = false;
            }

            public SerializerOptions UpdateFrom(BinarySerializedAttribute attribute)
            {
                return new()
                {
                    Endian = attribute.Endian ?? Endian,
                    StringEncoding = attribute.StringEncoding ?? StringEncoding,
                    LengthPropertyName = attribute.LengthPropertyName ?? LengthPropertyName,
                    NullTerminated = attribute.NullTerminated ?? NullTerminated
                };
            }
        }
    }
}
