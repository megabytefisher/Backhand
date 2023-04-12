using Backhand.Common.Buffers;
using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class SequenceReaderExpressions<T> where T : unmanaged, IEquatable<T>
    {
        private static readonly MethodInfo Read = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.Read))?.MakeGenericMethod(typeof(T)) ?? throw new Exception("Couldn't find Read");
        private static readonly MethodInfo ReadTo = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadTo))?.MakeGenericMethod(typeof(T)) ?? throw new Exception("Couldn't find ReadTo");
        private static readonly MethodInfo Peek = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.Peek))?.MakeGenericMethod(typeof(T)) ?? throw new Exception("Couldn't find Peek");
        private static readonly MethodInfo AdvanceTo = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.AdvanceTo))?.MakeGenericMethod(typeof(T)) ?? throw new Exception("Couldn't find AdvanceTo");
        private static readonly MethodInfo Advance = typeof(System.Buffers.SequenceReader<T>).GetMethod(nameof(System.Buffers.SequenceReader<T>.Advance)) ?? throw new Exception("Couldn't find Advance");
        private static readonly PropertyInfo Position = typeof(System.Buffers.SequenceReader<T>).GetProperty(nameof(System.Buffers.SequenceReader<T>.Position)) ?? throw new Exception("Couldn't find Position");
        private static readonly PropertyInfo Sequence = typeof(System.Buffers.SequenceReader<T>).GetProperty(nameof(System.Buffers.SequenceReader<T>.Sequence)) ?? throw new Exception("Couldn't find Sequence");

        public static Expression GetReadExpression(Expression bufferReader) => Expression.Call(Read, bufferReader);
        public static Expression GetReadToExpression(Expression bufferReader, Expression delimiter, Expression advancePastDelimiter) => Expression.Call(ReadTo, bufferReader, delimiter, advancePastDelimiter);
        public static Expression GetPeekExpression(Expression bufferReader, Expression? offset = null) => Expression.Call(Peek, bufferReader, offset ?? Expression.Constant(0L));
        public static Expression GetAdvanceToExpression(Expression bufferReader, Expression delimiter, Expression advancePastDelimiter) => Expression.Call(AdvanceTo, bufferReader, delimiter, advancePastDelimiter);
        public static Expression GetAdvanceExpression(Expression bufferReader, Expression count) => Expression.Call(bufferReader, Advance, count);
        public static Expression GetPositionExpression(Expression bufferReader) => Expression.Property(bufferReader, Position);
        public static Expression GetSequenceExpression(Expression bufferReader) => Expression.Property(bufferReader, Sequence);
    }

    internal static class ByteSequenceReaderExpressions
    {
        private static readonly MethodInfo ReadUInt16LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt16LittleEndian)) ?? throw new Exception("Couldn't find ReadUInt16LittleEndian");
        private static readonly MethodInfo ReadInt16LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt16LittleEndian)) ?? throw new Exception("Couldn't find ReadInt16LittleEndian");
        private static readonly MethodInfo ReadUInt16BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt16BigEndian)) ?? throw new Exception("Couldn't find ReadUInt16BigEndian");
        private static readonly MethodInfo ReadInt16BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt16BigEndian)) ?? throw new Exception("Couldn't find ReadInt16BigEndian");
        private static readonly MethodInfo ReadUInt32LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt32LittleEndian)) ?? throw new Exception("Couldn't find ReadUInt32LittleEndian");
        private static readonly MethodInfo ReadInt32LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt32LittleEndian)) ?? throw new Exception("Couldn't find ReadInt32LittleEndian");
        private static readonly MethodInfo ReadUInt32BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt32BigEndian)) ?? throw new Exception("Couldn't find ReadUInt32BigEndian");
        private static readonly MethodInfo ReadInt32BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt32BigEndian)) ?? throw new Exception("Couldn't find ReadInt32BigEndian");
        private static readonly MethodInfo ReadUInt64LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt64LittleEndian)) ?? throw new Exception("Couldn't find ReadUInt64LittleEndian");
        private static readonly MethodInfo ReadInt64LittleEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt64LittleEndian)) ?? throw new Exception("Couldn't find ReadInt64LittleEndian");
        private static readonly MethodInfo ReadUInt64BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadUInt64BigEndian)) ?? throw new Exception("Couldn't find ReadUInt64BigEndian");
        private static readonly MethodInfo ReadInt64BigEndian = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.ReadInt64BigEndian)) ?? throw new Exception("Couldn't find ReadInt64BigEndian");

        public static Expression GetReadUInt16LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt16LittleEndian, bufferReader);
        public static Expression GetReadInt16LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadInt16LittleEndian, bufferReader);
        public static Expression GetReadUInt16BigEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt16BigEndian, bufferReader);
        public static Expression GetReadInt16BigEndianExpression(Expression bufferReader) => Expression.Call(ReadInt16BigEndian, bufferReader);
        public static Expression GetReadUInt32LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt32LittleEndian, bufferReader);
        public static Expression GetReadInt32LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadInt32LittleEndian, bufferReader);
        public static Expression GetReadUInt32BigEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt32BigEndian, bufferReader);
        public static Expression GetReadInt32BigEndianExpression(Expression bufferReader) => Expression.Call(ReadInt32BigEndian, bufferReader);
        public static Expression GetReadUInt64LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt64LittleEndian, bufferReader);
        public static Expression GetReadInt64LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadInt64LittleEndian, bufferReader);
        public static Expression GetReadUInt64BigEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt64BigEndian, bufferReader);
        public static Expression GetReadInt64BigEndianExpression(Expression bufferReader) => Expression.Call(ReadInt64BigEndian, bufferReader);
    }
}
