namespace Backhand.Common.Buffers
{
    public class BufferWriteException : BufferException
    {
        public BufferWriteException()
            : base("Buffer write failed")
        {
        }
    }
}
