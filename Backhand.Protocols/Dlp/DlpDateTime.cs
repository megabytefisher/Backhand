using System;
using Backhand.Common.BinarySerialization;

namespace Backhand.Protocols.Dlp
{
    [GenerateBinarySerialization]
    public partial class DlpDateTime : IBinarySerializable
    {
        [BinarySerialize]
        public ushort Year { get; set; }

        [BinarySerialize]
        public byte Month { get; set; }
        
        [BinarySerialize]
        public byte Day { get; set; }
        
        [BinarySerialize]
        public byte Hour { get; set; }
        
        [BinarySerialize]
        public byte Minute { get; set; }
        
        [BinarySerialize]
        public byte Second { get; set; }
        
        [BinarySerialize]
        private byte Padding { get; set; }

        public DateTime AsDateTime
        {
            get => Year == 0 ? DateTime.MinValue : new(Year, Month, Day, Hour, Minute, Second);
            set
            {
                if (value == DateTime.MinValue)
                {
                    Year = 0;
                    Month = 0;
                    Day = 0;
                    Hour = 0;
                    Minute = 0;
                    Second = 0;
                    return;
                }

                Year = (ushort)value.Year;
                Month = (byte)value.Month;
                Day = (byte)value.Day;
                Hour = (byte)value.Hour;
                Minute = (byte)value.Minute;
                Second = (byte)value.Second;
            }
        }

        public override string ToString()
        {
            return $"{Year}-{Month}-{Day} {Hour}:{Minute}:{Second}";
        }

        public static implicit operator DateTime(DlpDateTime dlpDateTime)
        {
            return dlpDateTime.AsDateTime;
        }

        public static implicit operator DlpDateTime(DateTime dateTime)
        {
            return new() { AsDateTime = dateTime };
        }
    }
}