using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class EncodingExpressions
    {
        private static readonly MethodInfo GetBytes = typeof(EncodingExtensions).GetMethod(nameof(EncodingExtensions.GetBytes), BindingFlags.Public | BindingFlags.Static, new[] { typeof(Encoding), typeof(ReadOnlySequence<char>).MakeByRefType(), typeof(Span<byte>) }) ?? throw new Exception("Couldn't find GetBytes");
        private static readonly MethodInfo GetString = typeof(EncodingExtensions).GetMethod(nameof(EncodingExtensions.GetString), BindingFlags.Public | BindingFlags.Static, new[] { typeof(Encoding), typeof(ReadOnlySequence<byte>).MakeByRefType() }) ?? throw new Exception("Couldn't find GetString");
        private static readonly MethodInfo GetByteCount = typeof(Encoding).GetMethod(nameof(Encoding.GetByteCount), new[] { typeof(string) }) ?? throw new Exception("Couldn't find GetByteCount");
        
        public static Expression GetGetBytesExpression(Expression encoding, Expression charSequence, Expression byteSpan) => Expression.Call(GetBytes, encoding, charSequence, byteSpan);
        public static Expression GetGetStringExpression(Expression encoding, Expression byteSequence) => Expression.Call(GetString, encoding, byteSequence);
        public static Expression GetGetByteCountExpression(Expression encoding, Expression str) => Expression.Call(encoding, GetByteCount, str);
    }
}
