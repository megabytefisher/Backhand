using System.Linq.Expressions;
using System.Reflection;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class ArrayExpressions
    {
        // Find the generic CopyTo method on MemoryExtensions which accepts a T[] and Span<T>
        private static readonly MethodInfo CopyTo = typeof(MemoryExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(MemoryExtensions.CopyTo))
            .Where(m => m.GetParameters().Length == 2)
            .Where(m => m.GetParameters()[0].ParameterType.IsArray && (m.GetParameters()[0].ParameterType.GetElementType()?.IsGenericParameter ?? false))
            .Where(m => m.GetParameters()[1].ParameterType.IsGenericType && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Span<>))
            .Where(m => m.GetParameters()[0].ParameterType.GetElementType() == m.GetParameters()[1].ParameterType.GetGenericArguments()[0])
            .SingleOrDefault() ?? throw new Exception("Couldn't find CopyTo");

        public static Expression GetCopyToSpanExpression(Expression array, Expression span) => Expression.Call(CopyTo.MakeGenericMethod(typeof(byte)), array, span);
    }
}
