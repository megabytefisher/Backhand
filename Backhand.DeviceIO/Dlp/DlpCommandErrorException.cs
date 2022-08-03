using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpCommandErrorException : Exception
    {
        public DlpErrorCode ErrorCode { get; private init; }

        public DlpCommandErrorException(DlpErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }
    }
}
