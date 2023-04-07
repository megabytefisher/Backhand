using System.Buffers;
using System.Text;

namespace Backhand.Common.Buffers
{
    public static class HexSerialization
    {
        public static string GetHexString(ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length == 0)
                return string.Empty;

            StringBuilder result = new(Convert.ToInt32(buffer.Length) * 3 - 1);

            SequenceReader<byte> bufferReader = new(buffer);
            while (bufferReader.Remaining > 0)
            {
                byte value = bufferReader.Read();
                result.Append(value.ToString("X2"));

                if (bufferReader.Remaining > 0)
                {
                    result.Append(' ');
                }
            }

            return result.ToString();
        }

        public static string GetHexString(Span<byte> buffer)
        {
            if (buffer.Length == 0)
                return string.Empty;

            StringBuilder result = new(buffer.Length * 3 - 1);

            for (int i = 0; i < buffer.Length; i++)
            {
                byte value = buffer[i];
                result.Append(value.ToString("X2"));

                if (i < buffer.Length - 1)
                {
                    result.Append(' ');
                }
            }

            return result.ToString();
        }
    }
}
