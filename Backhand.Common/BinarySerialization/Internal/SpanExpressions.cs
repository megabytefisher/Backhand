using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal class SpanExpressions<T> : Expression
    {
        private static readonly MethodInfo Slice = typeof(Span<T>).GetMethod(nameof(Span<T>.Slice), new[] {  typeof(int), typeof(int) }) ?? throw new Exception("Couldn't find Slice");

        public static Expression GetSliceExpression(Expression span, Expression start, Expression length) => Call(span, Slice, start, length);
    }
}
