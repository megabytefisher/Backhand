using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class StringExpressions
    {
        private static readonly MethodInfo AsMemory = typeof(MemoryExtensions).GetMethod(nameof(MemoryExtensions.AsMemory), BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) }) ?? throw new Exception("Couldn't find AsMemory");
        private static readonly MethodInfo AsSpan = typeof(MemoryExtensions).GetMethod(nameof(MemoryExtensions.AsSpan), BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) }) ?? throw new Exception("Couldn't find AsSpan");

        public static Expression GetAsMemoryExpression(Expression str) => Expression.Call(AsMemory, str);
        public static Expression GetAsSpanExpression(Expression str) => Expression.Call(AsSpan, str);
    }
}
