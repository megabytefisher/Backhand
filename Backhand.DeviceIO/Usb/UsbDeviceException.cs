using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb
{
    public class UsbDeviceException : Exception
    {
        public UsbDeviceException(string message)
            : base(message)
        {
        }
    }
}
