using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class ReadOnlySequenceExpressions<T>
    {
        private static readonly ConstructorInfo FromReadOnlyMemory = typeof(ReadOnlySequence<T>).GetConstructor(new[] { typeof(ReadOnlyMemory<T>) }) ?? throw new Exception("Couldn't find constructor ReadOnlyMemory<T>");
        private static readonly MethodInfo SliceFromInts = typeof(ReadOnlySequence<T>).GetMethod(nameof(ReadOnlySequence<T>.Slice), new[] { typeof(int), typeof(int) }) ?? throw new Exception("Couldn't find method Slice");
        private static readonly MethodInfo SliceFromPositions = typeof(ReadOnlySequence<T>).GetMethod(nameof(ReadOnlySequence<T>.Slice), new[] { typeof(SequencePosition), typeof(SequencePosition) }) ?? throw new Exception("Couldn't find method Slice");
        private static readonly MethodInfo GetPositionFromInt64AndPosition = typeof(ReadOnlySequence<T>).GetMethod(nameof(ReadOnlySequence<T>.GetPosition), new[] { typeof(long), typeof(SequencePosition) }) ?? throw new Exception("Couldn't find method GetPosition");
        private static readonly PropertyInfo Length = typeof(ReadOnlySequence<T>).GetProperty(nameof(ReadOnlySequence<T>.Length)) ?? throw new Exception("Couldn't find property Length");

        public static Expression GetFromReadOnlyMemoryExpression(Expression readOnlyMemory) => Expression.New(FromReadOnlyMemory, readOnlyMemory);
        public static Expression GetSliceFromIntsExpression(Expression readOnlySequence, Expression start, Expression length) => Expression.Call(readOnlySequence, SliceFromInts, start, length);
        public static Expression GetSliceFromPositionsExpression(Expression readOnlySequence, Expression start, Expression end) => Expression.Call(readOnlySequence, SliceFromPositions, start, end);
        public static Expression GetGetPositionFromInt64AndPositionExpression(Expression readOnlySequence, Expression offset, Expression position) => Expression.Call(readOnlySequence, GetPositionFromInt64AndPosition, offset, position);
        public static Expression GetLengthExpression(Expression readOnlySequence) => Expression.Property(readOnlySequence, Length);
    }
}
