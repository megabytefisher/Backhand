using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public enum DlpOpcode
    {
        ReadUserInfo = 0x10,
        ReadSysInfo = 0x12,
        EndOfSync = 0x2f,
    }
}
