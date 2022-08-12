using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public enum DlpOpcode
    {
        /*** DLP 1.0 (PalmOS v1.0) ***/
        ReadUserInfo                = 0x10,
        ReadSysInfo                 = 0x12,
        ReadDbList                  = 0x16,
        OpenDb                      = 0x17,
        CreateDb                    = 0x18,
        CloseDb                     = 0x19,
        ReadAppBlock                = 0x1b,
        WriteAppBlock               = 0x1c,
        ReadSortBlock               = 0x1d,
        WriteSortBlock              = 0x1e,
        ReadRecord                  = 0x20,
        ReadResource                = 0x23,
        WriteResource               = 0x24,
        OpenConduit                 = 0x2e,
        EndOfSync                   = 0x2f,
        ReadRecordIdList            = 0x31,
    }
}
