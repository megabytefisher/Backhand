using Backhand.Common.BinarySerialization.Internal;
using Backhand.Common.Buffers;
using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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

                if (!string.IsNullOrEmpty(propertyOptions.ConditionName))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionName) ?? throw new Exception($"Condition property {propertyOptions.ConditionName} not found");

                    result = Expression.IfThenElse(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        Expression.Add(result, GetSizeExpression(propertyInfo.PropertyType, propertyOptions, Expression.Property(value, propertyInfo))),
                        result);
                }
                else
                {
                    result = Expression.Add(result, GetSizeExpression(propertyInfo.PropertyType, propertyOptions, Expression.Property(value, propertyInfo)));
                }
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

                if (!string.IsNullOrEmpty(propertyOptions.ConditionName))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionName) ?? throw new Exception($"Condition property {propertyOptions.ConditionName} not found");

                    bodyItems.Add(Expression.IfThen(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        GetWriteExpression(propertyInfo.PropertyType, bufferWriter, propertyOptions, Expression.Property(value, propertyInfo))));
                }
                else
                {
                    bodyItems.Add(GetWriteExpression(propertyInfo.PropertyType, bufferWriter, propertyOptions, Expression.Property(value, propertyInfo)));
                }
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

                if (!string.IsNullOrEmpty(propertyOptions.ConditionName))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionName) ?? throw new Exception($"Condition property {propertyOptions.ConditionName} not found");

                    bodyItems.Add(Expression.IfThen(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        GetReadExpression(value, Expression.Property(value, propertyInfo), propertyInfo.PropertyType, bufferReader, propertyOptions)));
                }
                else
                {
                    bodyItems.Add(GetReadExpression(value, Expression.Property(value, propertyInfo), propertyInfo.PropertyType, bufferReader, propertyOptions));
                }
            }

            BlockExpression body = Expression.Block(bodyItems);

            Expression myExp = Expression.Lambda<DeserializeImplementation>(body, bufferReader, value);

            return Expression.Lambda<DeserializeImplementation>(body, bufferReader, value).Compile();
        }

        private static Expression GetSizeExpression(Type type, SerializerOptions options, Expression value)
        {
            Expression? primitiveSize = GetPrimitiveSizeExpression(type);

            if (primitiveSize != null)
            {
                return primitiveSize;
            }
            else if (type == typeof(string))
            {
                Encoding encoding = GetEncoding(options.StringEncoding);

                Expression stringLength = EncodingExpressions.GetGetByteCountExpression(Expression.Constant(encoding), value);

                if (options.NullTerminated)
                {
                    stringLength = Expression.Add(stringLength, Expression.Constant(encoding.GetByteCount("\0")));
                }

                return stringLength;
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
            Expression? primitiveWrite = GetPrimitiveWriteExpression(writeType, value, bufferWriter, options);

            if (primitiveWrite != null)
            {
                return primitiveWrite;
            }
            else if (writeType == typeof(string))
            {
                Encoding encoding = GetEncoding(options.StringEncoding);

                List<Expression> bodyItems = new List<Expression>
                {
                    SpanWriterExpressions<byte>.GetAdvanceExpression(
                        bufferWriter,
                        EncodingExpressions.GetGetBytesExpression(
                            encoding: Expression.Constant(encoding),
                            charSequence: ReadOnlySequenceExpressions<char>.GetFromReadOnlyMemoryExpression(StringExpressions.GetAsMemoryExpression(value)),
                            byteSpan: Expression.Property(bufferWriter, nameof(SpanWriter<byte>.RemainingSpan))
                        )
                    )
                };

                if (options.NullTerminated)
                {
                    byte[] nullBytes = encoding.GetBytes("\0");

                    foreach (byte nullByte in nullBytes)
                    {
                        bodyItems.Add(SpanWriterExpressions<byte>.GetWriteExpression(bufferWriter, Expression.Constant(nullByte)));
                    }
                }

                return Expression.Block(
                    bodyItems
                );
            }
            else if (writeType.IsArray)
            {
                Type elementType = writeType.GetElementType() ?? throw new Exception("Couldn't get array property element type");
                Expression arrayLength = Expression.ArrayLength(value);

                if (elementType == typeof(byte))
                {
                    return Expression.Block(
                        ArrayExpressions.GetCopyToSpanExpression(value, Expression.Property(bufferWriter, nameof(SpanWriter<byte>.RemainingSpan))),
                        SpanWriterExpressions<byte>.GetAdvanceExpression(bufferWriter, arrayLength)
                    );
                }

                LabelTarget loopEnd = Expression.Label("loopEnd");
                ParameterExpression i = Expression.Variable(typeof(int), "i");

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

        private static Expression GetReadExpression(Expression containerObject, Expression value, Type readType, ParameterExpression bufferReader, SerializerOptions options)
        {
            Expression? primitiveRead = GetPrimitiveReadExpression(readType, bufferReader, options);

            if (primitiveRead != null)
            {
                return Expression.Assign(value, primitiveRead);
            }
            else if (readType == typeof(string))
            {
                Encoding encoding = GetEncoding(options.StringEncoding);

                if (options.NullTerminated)
                {
                    byte[] nullBytes = encoding.GetBytes("\0");

                    LabelTarget loopEnd = Expression.Label("loopEnd");
                    ParameterExpression start = Expression.Variable(typeof(SequencePosition), "start");
                    ParameterExpression end = Expression.Variable(typeof(SequencePosition), "end");

                    Expression peekChecks = Expression.Constant(true);

                    for (int i = 1; i < nullBytes.Length - 1; i++)
                    {
                        peekChecks = Expression.AndAlso(
                            peekChecks,
                            Expression.Equal(
                                SequenceReaderExpressions<byte>.GetPeekExpression(bufferReader, Expression.Constant(i)),
                                Expression.Constant(nullBytes[i])
                            )
                        );
                    }

                    return Expression.Block(
                        new[] { start, end },
                        Expression.Assign(start, SequenceReaderExpressions<byte>.GetPositionExpression(bufferReader)),
                        Expression.Loop(
                            Expression.Block(
                                Expression.IfThen(
                                    Expression.Not(Expression.Equal(SequenceReaderExpressions<byte>.GetPeekExpression(bufferReader), Expression.Constant(nullBytes[0]))),
                                    SequenceReaderExpressions<byte>.GetAdvanceToExpression(bufferReader, Expression.Constant(nullBytes[0]), Expression.Constant(false))
                                ),
                                Expression.IfThenElse(
                                    peekChecks,
                                    Expression.Block(
                                        Expression.Assign(
                                            end,
                                            SequenceReaderExpressions<byte>.GetPositionExpression(bufferReader)
                                        ),
                                        SequenceReaderExpressions<byte>.GetAdvanceExpression(bufferReader, Expression.Constant((long)nullBytes.Length)),
                                        Expression.Assign(
                                            value,
                                            EncodingExpressions.GetGetStringExpression(
                                                Expression.Constant(encoding),
                                                ReadOnlySequenceExpressions<byte>.GetSliceFromPositionsExpression(
                                                    SequenceReaderExpressions<byte>.GetSequenceExpression(bufferReader),
                                                    start,
                                                    end
                                                )
                                            )
                                        ),
                                        Expression.Break(loopEnd)
                                    ),
                                    SequenceReaderExpressions<byte>.GetAdvanceExpression(bufferReader, Expression.Constant(1L))
                                )
                            ),
                            loopEnd
                        )
                    );
                }
                else if (!string.IsNullOrEmpty(options.LengthName))
                {
                    PropertyInfo lengthProperty = typeof(T).GetProperty(options.LengthName) ?? throw new Exception("Couldn't get property specified by LengthName");
                    Expression length = Expression.Property(containerObject, lengthProperty);

                    return Expression.Block(
                        Expression.Assign(
                            value,
                            EncodingExpressions.GetGetStringExpression(
                                Expression.Constant(encoding),
                                ReadOnlySequenceExpressions<byte>.GetSliceFromIntsExpression(Expression.Property(bufferReader, nameof(SequenceReader<byte>.UnreadSequence)), Expression.Constant(0), length))),
                        SequenceReaderExpressions<byte>.GetAdvanceExpression(bufferReader, Expression.Convert(length, typeof(long)))
                    );
                }

                throw new Exception("Unhandled string");
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
                                GetReadExpression(containerObject, Expression.ArrayAccess(value, i), elementType, bufferReader, options),
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

        private static Expression? GetPrimitiveSizeExpression(Type type) => Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => Expression.Constant(sizeof(byte)),
            TypeCode.UInt16 => Expression.Constant(sizeof(ushort)),
            TypeCode.Int16 => Expression.Constant(sizeof(short)),
            TypeCode.UInt32 => Expression.Constant(sizeof(uint)),
            TypeCode.Int32 => Expression.Constant(sizeof(int)),
            TypeCode.UInt64 => Expression.Constant(sizeof(ulong)),
            TypeCode.Int64 => Expression.Constant(sizeof(long)),
            _ => null
        };

        private static Expression? GetPrimitiveWriteExpression(Type writeType, Expression value, Expression bufferWriter, SerializerOptions options) => Type.GetTypeCode(writeType) switch
        {
            TypeCode.Byte => SpanWriterExpressions<byte>.GetWriteExpression(bufferWriter, Expression.Convert(value, typeof(byte))),
            TypeCode.UInt16 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteUInt16LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(ushort))) :
                ByteSpanWriterExpressions.GetWriteUInt16BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(ushort))),
            TypeCode.Int16 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteInt16LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(short))) :
                ByteSpanWriterExpressions.GetWriteInt16BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(ushort))),
            TypeCode.UInt32 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteUInt32LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(uint))) :
                ByteSpanWriterExpressions.GetWriteUInt32BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(uint))),
            TypeCode.Int32 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteInt32LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(int))) :
                ByteSpanWriterExpressions.GetWriteInt32BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(int))),
            TypeCode.UInt64 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteUInt64LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(ulong))) :
                ByteSpanWriterExpressions.GetWriteUInt64BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(ulong))),
            TypeCode.Int64 => options.Endian == Endian.Little ?
                ByteSpanWriterExpressions.GetWriteInt64LittleEndianExpression(bufferWriter, Expression.Convert(value, typeof(long))) :
                ByteSpanWriterExpressions.GetWriteInt64BigEndianExpression(bufferWriter, Expression.Convert(value, typeof(long))),
            _ => null
        };

        private static Expression? GetPrimitiveReadExpression(Type readType, Expression bufferReader, SerializerOptions options)
        {
            Expression? readExpression = Type.GetTypeCode(readType) switch
            {
                TypeCode.Byte => SequenceReaderExpressions<byte>.GetReadExpression(bufferReader),
                TypeCode.UInt16 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadUInt16LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadUInt16BigEndianExpression(bufferReader),
                TypeCode.Int16 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadInt16LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadInt16BigEndianExpression(bufferReader),
                TypeCode.UInt32 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadUInt32LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadUInt32BigEndianExpression(bufferReader),
                TypeCode.Int32 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadInt32LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadInt32BigEndianExpression(bufferReader),
                TypeCode.UInt64 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadUInt64LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadUInt64BigEndianExpression(bufferReader),
                TypeCode.Int64 => options.Endian == Endian.Little ?
                    ByteSequenceReaderExpressions.GetReadInt64LittleEndianExpression(bufferReader) :
                    ByteSequenceReaderExpressions.GetReadInt64BigEndianExpression(bufferReader),
                _ => null
            };

            if (readExpression == null)
            {
                return null;
            }

            return Expression.Convert(readExpression, readType);
        }

        private static Encoding GetEncoding(StringEncoding encoding) => encoding switch
        {
            StringEncoding.ASCII => Encoding.ASCII,
            _ => throw new Exception("Unknown string encoding")
        };

        private ref struct SerializerOptions
        {
            public Endian Endian { get; private init; }
            public StringEncoding StringEncoding { get; private init; }
            public string LengthName { get; private init; }
            public string ConditionName { get; private init; }
            public bool NullTerminated { get; private init; }

            public SerializerOptions()
            {
                Endian = Endian.Big;
                StringEncoding = StringEncoding.ASCII;
                LengthName = string.Empty;
                ConditionName = string.Empty;
                NullTerminated = false;
            }

            public SerializerOptions UpdateFrom(BinarySerializedAttribute attribute)
            {
                return new()
                {
                    Endian = attribute.EndianSpecified ? attribute.Endian : Endian,
                    StringEncoding = attribute.StringEncodingSpecified ? attribute.StringEncoding : StringEncoding,
                    LengthName = attribute.LengthNameSpecified ? attribute.LengthName : LengthName,
                    ConditionName = attribute.ConditionNameSpecified ? attribute.ConditionName : ConditionName,
                    NullTerminated = attribute.NullTerminatedSpecified ? attribute.NullTerminated : NullTerminated
                };
            }
        }
    }
}
