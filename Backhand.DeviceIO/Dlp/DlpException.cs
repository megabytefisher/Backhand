using System;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpException : Exception
    {
        public DlpException(string message)
            : base(message)
        {
        }
    }
}
