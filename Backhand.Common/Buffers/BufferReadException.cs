namespace Backhand.Common.Buffers
{
    public class BufferReadException : BufferException
    {
        public BufferReadException()
            : base("Buffer read failed")
        {
        }
    }
}
