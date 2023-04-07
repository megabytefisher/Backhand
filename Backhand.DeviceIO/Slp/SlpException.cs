using System;

namespace Backhand.DeviceIO.Slp
{
    public class SlpException : Exception
    {
        public SlpException(string message)
            : base(message)
        {
        }
    }
}
