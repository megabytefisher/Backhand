using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands
{
    public class DlpCommandException : Exception
    {
        public DlpCommandException(string message)
            : base(message)
        {
        }
    }
}
