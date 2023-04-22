﻿using Backhand.Common.BinarySerialization.Internal;
using Backhand.Common.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Backhand.Common.BinarySerialization
{
    public static class BinarySerializer<T> where T : class
    {
        private delegate int GetSizeImplementation(T value);
        private delegate void SerializeImplementation(T value, ref SpanWriter<byte> bufferWriter);
        private delegate void DeserializeImplementation(ref SequenceReader<byte> bufferReader, T value);

        private static readonly Lazy<GetSizeImplementation> _getSize = new(BuildGetSize);
        private static readonly Lazy<SerializeImplementation> _serialize = new(BuildSerialize);
        private static readonly Lazy<DeserializeImplementation> _deserialize = new(BuildDeserialize);
        private static T? _defaultInstance;

        private const BindingFlags DefaultPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int GetSize(T value)
        {
            if (value is ICustomBinarySerializable customSerializable)
            {
                return customSerializable.GetSize();
            }

            return _getSize.Value(value);
        }

        public static int GetMinimumSize<TItem>() where TItem : T, new()
        {
            if (_defaultInstance == null)
            {
                _defaultInstance = new TItem();
            }

            return GetSize(_defaultInstance);
        }

        public static void Serialize(T value, ref SpanWriter<byte> buffer)
        {
            if (value is ICustomBinarySerializable customSerializable)
            {
                customSerializable.Serialize(ref buffer);
                return;
            }

            _serialize.Value(value, ref buffer);
        }

        public static void Serialize(T value, Span<byte> buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);
            Serialize(value, ref bufferWriter);
        }

        public static void Serialize(T value, byte[] buffer)
        {
            SpanWriter<byte> bufferWriter = new(buffer);
            Serialize(value, ref bufferWriter);
        }

        public static void Deserialize(ref SequenceReader<byte> buffer, T value)
        {
            if (value is ICustomBinarySerializable customSerializable)
            {
                customSerializable.Deserialize(ref buffer);
                return;
            }

            _deserialize.Value(ref buffer, value);
        }

        public static void Deserialize(ReadOnlySequence<byte> buffer, T value)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);
            Deserialize(ref bufferReader, value);
        }

        public static void Deserialize(byte[] buffer, T value)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
            Deserialize(ref bufferReader, value);
        }

        private static GetSizeImplementation BuildGetSize()
        {
            BinarySerializableAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializableAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The only parameter is a T (value)
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));

            // We need to add code to calculate the size of each BinarySerialized property
            Expression result = Expression.Constant(0);

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(DefaultPropertyFlags))
            {
                BinarySerializeAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializeAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                if (!string.IsNullOrEmpty(propertyOptions.ConditionProperty))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionProperty) ?? throw new Exception($"Condition property {propertyOptions.ConditionProperty} not found");

                    result = Expression.IfThenElse(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        Expression.Add(result, GetSizeExpression(propertyInfo.PropertyType, Expression.Property(value, propertyInfo))),
                        result);
                }
                else
                {
                    result = Expression.Add(result, GetSizeExpression(propertyInfo.PropertyType, Expression.Property(value, propertyInfo)));
                }
            }

            if (!string.IsNullOrEmpty(defaultOptions.MinimumLengthProperty))
            {
                PropertyInfo lengthProperty = typeof(T).GetProperty(defaultOptions.MinimumLengthProperty) ?? throw new Exception($"Length property {defaultOptions.MinimumLengthProperty} not found on type {typeof(T).FullName}");
                Expression minimumLength = Expression.Property(value, lengthProperty);

                result = Expression.IfThenElse(
                    Expression.GreaterThan(minimumLength, result),
                    minimumLength,
                    result
                );
            }

            return Expression.Lambda<GetSizeImplementation>(result, value).Compile();
        }

        private static SerializeImplementation BuildSerialize()
        {
            BinarySerializableAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializableAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The two parameters are a T (value) and a reference to a SpanWriter<byte> (bufferWriter)
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));
            ParameterExpression bufferWriter = Expression.Parameter(typeof(SpanWriter<byte>).MakeByRefType(), nameof(bufferWriter));

            // Local variables
            ParameterExpression startIndex = Expression.Variable(typeof(int), nameof(startIndex));

            // List of body expressions
            List<Expression> bodyItems = new List<Expression>();

            bodyItems.Add(
                Expression.Assign(
                    startIndex,
                    SpanWriterExpressions<byte>.GetIndexExpression(bufferWriter)
                )
            );

            // We need to add code to serialize each BinarySerialized property
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(DefaultPropertyFlags))
            {
                BinarySerializeAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializeAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                if (!string.IsNullOrEmpty(propertyOptions.ConditionProperty))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionProperty) ?? throw new Exception($"Condition property {propertyOptions.ConditionProperty} not found");

                    bodyItems.Add(Expression.IfThen(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        GetWriteExpression(propertyInfo.PropertyType, bufferWriter, propertyOptions, Expression.Property(value, propertyInfo))));
                }
                else
                {
                    bodyItems.Add(GetWriteExpression(propertyInfo.PropertyType, bufferWriter, propertyOptions, Expression.Property(value, propertyInfo)));
                }
            }

            if (!string.IsNullOrEmpty(defaultOptions.MinimumLengthProperty))
            {
                PropertyInfo minimumLengthProperty = typeof(T).GetProperty(defaultOptions.MinimumLengthProperty) ?? throw new Exception($"Length property {defaultOptions.MinimumLengthProperty} not found on type {typeof(T).FullName}");
                Expression minimumLength = Expression.Property(value, minimumLengthProperty);

                Expression writtenLength = Expression.Subtract(SpanWriterExpressions<byte>.GetIndexExpression(bufferWriter), startIndex);

                LabelTarget loopEnd = Expression.Label(nameof(loopEnd));

                bodyItems.Add(
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(writtenLength, Expression.Convert(minimumLength, typeof(int))),
                            SpanWriterExpressions<byte>.GetWriteExpression(bufferWriter, Expression.Constant(0)),
                            Expression.Break(loopEnd)
                        ),
                        loopEnd
                    )
                );
            }

            BlockExpression body = Expression.Block(new[] { startIndex }, bodyItems);

            return Expression.Lambda<SerializeImplementation>(body, value, bufferWriter).Compile();
        }

        private static DeserializeImplementation BuildDeserialize()
        {
            BinarySerializableAttribute objectSerializedAttribute = typeof(T).GetCustomAttribute<BinarySerializableAttribute>() ?? throw new Exception("Type must have BinarySerializedAttribute");
            SerializerOptions defaultOptions = new SerializerOptions().UpdateFrom(objectSerializedAttribute);

            // The two parameters are a SequenceReader<byte> (reader) and a reference to a T (value)
            ParameterExpression bufferReader = Expression.Parameter(typeof(SequenceReader<byte>).MakeByRefType(), nameof(bufferReader));
            ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));

            // List of body expressions
            List<Expression> bodyItems = new List<Expression>();

            // Local variables
            ParameterExpression startOffset = Expression.Variable(typeof(long), nameof(startOffset));

            bodyItems.Add(
                Expression.Assign(
                    startOffset,
                    ReadOnlySequenceExpressions<byte>.GetGetOffsetExpression(
                        SequenceReaderExpressions<byte>.GetSequenceExpression(bufferReader),
                        SequenceReaderExpressions<byte>.GetPositionExpression(bufferReader)
                    )
                )
            );

            // We need to add code to deserialize each BinarySerialized property
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties(DefaultPropertyFlags))
            {
                BinarySerializeAttribute? serializedAttribute;
                if ((serializedAttribute = propertyInfo.GetCustomAttribute<BinarySerializeAttribute>()) == null)
                {
                    continue;
                }

                SerializerOptions propertyOptions = defaultOptions.UpdateFrom(serializedAttribute);

                if (!string.IsNullOrEmpty(propertyOptions.ConditionProperty))
                {
                    PropertyInfo conditionProperty = typeof(T).GetProperty(propertyOptions.ConditionProperty) ?? throw new Exception($"Condition property {propertyOptions.ConditionProperty} not found");

                    bodyItems.Add(Expression.IfThen(
                        Expression.IsTrue(Expression.Property(value, conditionProperty)),
                        GetReadExpression(Expression.Property(value, propertyInfo), propertyInfo.PropertyType, bufferReader, propertyOptions)));
                }
                else
                {
                    bodyItems.Add(GetReadExpression(Expression.Property(value, propertyInfo), propertyInfo.PropertyType, bufferReader, propertyOptions));
                }
            }

            if (!string.IsNullOrEmpty(defaultOptions.MinimumLengthProperty))
            {
                PropertyInfo minimumLengthProperty = typeof(T).GetProperty(defaultOptions.MinimumLengthProperty, DefaultPropertyFlags) ?? throw new Exception($"Length property {defaultOptions.MinimumLengthProperty} not found on type {typeof(T).FullName}");
                Expression minimumLength = Expression.Property(value, minimumLengthProperty);

                Expression readLength = Expression.Subtract(
                    ReadOnlySequenceExpressions<byte>.GetGetOffsetExpression(
                        SequenceReaderExpressions<byte>.GetSequenceExpression(bufferReader),
                        SequenceReaderExpressions<byte>.GetPositionExpression(bufferReader)
                    ),
                    startOffset
                );

                LabelTarget loopEnd = Expression.Label(nameof(loopEnd));

                bodyItems.Add(
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(readLength, Expression.Convert(minimumLength, typeof(long))),
                            SequenceReaderExpressions<byte>.GetAdvanceExpression(bufferReader, Expression.Constant(1L)),
                            Expression.Break(loopEnd)
                        ),
                        loopEnd
                    )
                );
            }

            BlockExpression body = Expression.Block(
                new[] { startOffset },
                bodyItems
            );

            return Expression.Lambda<DeserializeImplementation>(body, bufferReader, value).Compile();
        }

        private static Expression GetSizeExpression(Type type, Expression value)
        {
            Expression? primitiveSize = GetPrimitiveSizeExpression(type);

            if (primitiveSize != null)
            {
                return primitiveSize;
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
                                Expression.AddAssign(result, GetSizeExpression(elementType, Expression.ArrayAccess(value, i))),
                                Expression.PostIncrementAssign(i)
                            ),
                            Expression.Break(loopEnd, result)
                        ),
                        loopEnd
                    )
                );
            }
            else if (type.GetCustomAttribute<BinarySerializableAttribute>() != null)
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
            else if (writeType.GetCustomAttribute<BinarySerializableAttribute>() != null)
            {
                Type propertySerializerType = typeof(BinarySerializer<>).MakeGenericType(writeType);
                MethodInfo propertySerializeMethod = propertySerializerType.GetMethod(nameof(Serialize), new[] { writeType, typeof(SpanWriter<byte>).MakeByRefType() }) ?? throw new Exception("Couldn't get Serialize method for property type");

                return Expression.Call(null, propertySerializeMethod, value, bufferWriter);
            }
            else
            {
                throw new Exception("Unknown BinarySerialized property type");
            }
        }

        private static Expression GetReadExpression(Expression value, Type readType, ParameterExpression bufferReader, SerializerOptions options)
        {
            Expression? primitiveRead = GetPrimitiveReadExpression(readType, bufferReader, options);

            if (primitiveRead != null)
            {
                return Expression.Assign(value, primitiveRead);
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
                                GetReadExpression(Expression.ArrayAccess(value, i), elementType, bufferReader, options),
                                Expression.PostIncrementAssign(i)
                            ),
                            Expression.Break(loopEnd)
                        ),
                        loopEnd
                    )
                );
            }
            else if (readType.GetCustomAttribute<BinarySerializableAttribute>() != null)
            {
                Type propertySerializerType = typeof(BinarySerializer<>).MakeGenericType(readType);
                MethodInfo propertyDeserializeMethod = propertySerializerType.GetMethod(nameof(Deserialize), new[] { typeof(SequenceReader<byte>).MakeByRefType(), readType }) ?? throw new Exception("Couldn't get Deserialize method for property type");
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
            TypeCode.Boolean => Expression.Constant(sizeof(byte)),
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
            TypeCode.Boolean => SpanWriterExpressions<byte>.GetWriteExpression(bufferWriter, Expression.IfThenElse(value, Expression.Constant((byte)1), Expression.Constant((byte)0))),
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
                TypeCode.Boolean => Expression.NotEqual(SequenceReaderExpressions<byte>.GetReadExpression(bufferReader), Expression.Constant((byte)0)),
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
            public Endian Endian { get; private set; }
            public string MinimumLengthProperty { get; private set; }
            public string ConditionProperty { get; private set; }

            public SerializerOptions()
            {
                Endian = Endian.Big;
                MinimumLengthProperty = string.Empty;
                ConditionProperty = string.Empty;
            }

            public SerializerOptions UpdateFrom(BinarySerializableAttribute attribute)
            {
                SerializerOptions newOptions = this;

                newOptions.Endian = attribute.EndianSpecified ? attribute.Endian : Endian;
                newOptions.MinimumLengthProperty = attribute.MinimumLengthPropertySpecified ? attribute.MinimumLengthProperty : string.Empty;
                newOptions.ConditionProperty = string.Empty;

                return newOptions;
            }

            public SerializerOptions UpdateFrom(BinarySerializeAttribute attribute)
            {
                SerializerOptions newOptions = this;

                newOptions.Endian = attribute.EndianSpecified ? attribute.Endian : Endian;
                newOptions.MinimumLengthProperty = string.Empty;
                newOptions.ConditionProperty = attribute.ConditionPropertySpecified ? attribute.ConditionProperty : string.Empty;

                return newOptions;
            }
        }
    }
}
