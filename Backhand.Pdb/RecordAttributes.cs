using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    [Flags]
    public enum RecordAttributes : byte
    {
        Delete          = 0b10000000,
        Dirty           = 0b01000000,
        Busy            = 0b00100000,
        Secret          = 0b00010000,
    }
}
