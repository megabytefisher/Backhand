using System;

namespace Backhand.Dlp.Commands
{
    [Flags]
    public enum DlpRecordAttributes
    {
        Delete          = 0b10000000,
        Dirty           = 0b01000000,
        Busy            = 0b00100000,
        Secret          = 0b00010000,
    }
}
