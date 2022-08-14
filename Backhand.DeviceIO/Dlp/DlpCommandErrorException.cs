using System;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpCommandErrorException : Exception
    {
        public DlpErrorCode ErrorCode { get; }

        public DlpCommandErrorException(DlpErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }
    }
}
