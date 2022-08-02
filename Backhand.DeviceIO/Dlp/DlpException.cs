using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
