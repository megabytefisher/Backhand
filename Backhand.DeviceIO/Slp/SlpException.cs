using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
