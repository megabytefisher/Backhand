using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal class SpanExpressions<T> : Expression
    {
        private static readonly MethodInfo Slice = typeof(Span<T>).GetMethod(nameof(Span<T>.Slice), new Type[] {  typeof(int), typeof(int) }) ?? throw new Exception("Couldn't find Slice");

        public static Expression GetSliceExpression(Expression span, Expression start, Expression length) => Expression.Call(span, Slice, start, length);
    }
}
