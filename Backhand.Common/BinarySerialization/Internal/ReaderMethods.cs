using Backhand.Common.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Common.BinarySerialization.Internal
{
    internal static class ReaderMethods
    {
        private static readonly MethodInfo Read = typeof(SequenceReaderExtensions).GetMethod(nameof(SequenceReaderExtensions.Read))?.MakeGenericMethod(typeof(byte)) ?? throw new Exception("Couldn't find Read");
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
        private static readonly MethodInfo Advance = typeof(System.Buffers.SequenceReader<byte>).GetMethod(nameof(System.Buffers.SequenceReader<byte>.Advance)) ?? throw new Exception("Couldn't find Advance");

        public static Expression GetReadExpression(Expression bufferReader) => Expression.Call(Read, bufferReader);
        public static Expression GetReadUInt16LittleEndianExpression(Expression bufferReader) => Expression.Call(ReadUInt16BigEndian, bufferReader);

    }
}
