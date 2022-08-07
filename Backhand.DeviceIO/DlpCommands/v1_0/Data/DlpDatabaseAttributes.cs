using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.DlpCommands.v1_0.Data
{
    [Flags]
    public enum DlpDatabaseAttributes : ushort
    {
        Open                = 0b10000000_00000000,
        Unused1             = 0b01000000_00000000,
        Unused2             = 0b00100000_00000000,
        Unused3             = 0b00010000_00000000,
        Bundle              = 0b00001000_00000000,
        Recyclable          = 0b00000100_00000000,
        LaunchableData      = 0b00000010_00000000,
        Hidden              = 0b00000001_00000000,
        Stream              = 0b00000000_10000000,
        CopyPrevention      = 0b00000000_01000000,
        ResetAfterInstall   = 0b00000000_00100000,
        OkToInstallNewer    = 0b00000000_00010000,
        Backup              = 0b00000000_00001000,
        AppInfoDirty        = 0b00000000_00000100,
        ReadOnly            = 0b00000000_00000010,
        ResourceDb          = 0b00000000_00000001,
    }
}
