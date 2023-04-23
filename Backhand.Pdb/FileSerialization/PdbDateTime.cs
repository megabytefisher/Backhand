using System;
using Backhand.Common.BinarySerialization;

namespace Backhand.Pdb.FileSerialization
{
    [GenerateBinarySerialization]
    internal partial class PdbDateTime : IBinarySerializable
    {
        [BinarySerialize]
        public uint Offset { get; set; }

        public bool IsPalmEpoch => (Offset & (1 << 31)) != 0;
        
        private static readonly DateTime PalmEpoch = new(1904, 1, 1);
        private static readonly DateTime UnixEpoch = new(1970, 1, 1);

        public DateTime AsDateTime
        {
            get
            {
                DateTime epoch = IsPalmEpoch ? PalmEpoch : UnixEpoch;
                return epoch.AddSeconds(Offset);
            }
            set
            {
                DateTime epoch = IsPalmEpoch ? PalmEpoch : UnixEpoch;

                if (value == DateTime.MinValue)
                {
                    Offset = 0;
                    return;
                }

                if (value < epoch && !IsPalmEpoch)
                {
                    epoch = PalmEpoch;
                }

                Offset = Convert.ToUInt32(value.Subtract(epoch).TotalSeconds);
            }
        }

        public static implicit operator DateTime(PdbDateTime pdbDateTime)
        {
            return pdbDateTime.AsDateTime;
        }

        public static implicit operator PdbDateTime(DateTime dateTime)
        {
            return new() { AsDateTime = dateTime };
        }
    }
}