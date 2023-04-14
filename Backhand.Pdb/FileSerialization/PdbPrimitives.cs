using System;

namespace Backhand.Pdb.FileSerialization
{
    internal static class PdbPrimitives
    {
        public enum EpochType
        {
            Palm,
            Unix
        }

        private static readonly DateTime PalmEpoch = new(1904, 1, 1);
        private static readonly DateTime UnixEpoch = new(1970, 1, 1);

        public static DateTime FromPdbDateTime(uint value)
        {
            bool isPalmEpoch = (value & (1 << 31)) != 0;
            DateTime epoch = isPalmEpoch ? PalmEpoch : UnixEpoch;
            return epoch.AddSeconds(value);
        }

        public static uint ToPdbDateTime(DateTime value, EpochType epochType = EpochType.Unix)
        {
            DateTime epoch = epochType == EpochType.Unix ? UnixEpoch : PalmEpoch;

            if (value == DateTime.MinValue)
                value = epoch;

            return Convert.ToUInt32(value.Subtract(epoch).TotalSeconds);
        }
    }
}
