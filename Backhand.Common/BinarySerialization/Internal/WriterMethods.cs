using Backhand.Common.Buffers;
using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class WriterMethods
    {
        private static readonly MethodInfo Write = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.Write))?.MakeGenericMethod(typeof(byte)) ?? throw new Exception("Couldn't find Write");
        private static readonly MethodInfo WriteUInt16LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt16LittleEndian)) ?? throw new Exception("Couldn't find WriteUInt16LittleEndian");
        private static readonly MethodInfo WriteInt16LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt16LittleEndian)) ?? throw new Exception("Couldn't find WriteInt16LittleEndian");
        private static readonly MethodInfo WriteUInt16BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt16BigEndian)) ?? throw new Exception("Couldn't find WriteUInt16BigEndian");
        private static readonly MethodInfo WriteInt16BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt16BigEndian)) ?? throw new Exception("Couldn't find WriteInt16BigEndian");
        private static readonly MethodInfo WriteUInt32LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt32LittleEndian)) ?? throw new Exception("Couldn't find WriteUInt32LittleEndian");
        private static readonly MethodInfo WriteInt32LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt32LittleEndian)) ?? throw new Exception("Couldn't find WriteInt32LittleEndian");
        private static readonly MethodInfo WriteUInt32BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt32BigEndian)) ?? throw new Exception("Couldn't find WriteUInt32BigEndian");
        private static readonly MethodInfo WriteInt32BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt32BigEndian)) ?? throw new Exception("Couldn't find WriteInt32BigEndian");
        private static readonly MethodInfo WriteUInt64LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt64LittleEndian)) ?? throw new Exception("Couldn't find WriteUInt64LittleEndian");
        private static readonly MethodInfo WriteInt64LittleEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt64LittleEndian)) ?? throw new Exception("Couldn't find WriteInt64LittleEndian");
        private static readonly MethodInfo WriteUInt64BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteUInt64BigEndian)) ?? throw new Exception("Couldn't find WriteUInt64BigEndian");
        private static readonly MethodInfo WriteInt64BigEndian = typeof(SpanWriterExtensions).GetMethod(nameof(SpanWriterExtensions.WriteInt64BigEndian)) ?? throw new Exception("Couldn't find WriteInt64BigEndian");
        private static readonly MethodInfo Advance = typeof(SpanWriter<byte>).GetMethod(nameof(SpanWriter<byte>.Advance)) ?? throw new Exception("Couldn't find Advance");

        public static Expression GetWriteExpression(Expression bufferWriter, Expression value) => Expression.Call(Write, bufferWriter, value);
        public static Expression GetWriteUInt16LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt16LittleEndian, bufferWriter, value);
        public static Expression GetWriteInt16LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt16LittleEndian, bufferWriter, value);
        public static Expression GetWriteUInt16BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt16BigEndian, bufferWriter, value);
        public static Expression GetWriteInt16BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt16BigEndian, bufferWriter, value);
        public static Expression GetWriteUInt32LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt32LittleEndian, bufferWriter, value);
        public static Expression GetWriteInt32LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt32LittleEndian, bufferWriter, value);
        public static Expression GetWriteUInt32BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt32BigEndian, bufferWriter, value);
        public static Expression GetWriteInt32BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt32BigEndian, bufferWriter, value);
        public static Expression GetWriteUInt64LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt64LittleEndian, bufferWriter, value);
        public static Expression GetWriteInt64LittleEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt64LittleEndian, bufferWriter, value);
        public static Expression GetWriteUInt64BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteUInt64BigEndian, bufferWriter, value);
        public static Expression GetWriteInt64BigEndianExpression(Expression bufferWriter, Expression value) => Expression.Call(WriteInt64BigEndian, bufferWriter, value);
        public static Expression GetAdvanceExpression(Expression bufferWriter, Expression value) => Expression.Call(bufferWriter, Advance, value);
    }
}
