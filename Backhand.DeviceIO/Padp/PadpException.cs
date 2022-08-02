using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Padp
{
    public class PadpException : Exception
    {
        public PadpException(string message)
            : base(message)
        {
        }
    }
}
