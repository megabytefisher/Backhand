namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpOpcodes
    {
        public const byte ReadUserInfo = 0x10;
        public const byte ReadSysInfo = 0x12;
        public const byte ReadDbList = 0x16;
        public const byte OpenDb = 0x17;
        public const byte CreateDb = 0x18;
        public const byte CloseDb = 0x19;
        public const byte DeleteDb = 0x1a;
        public const byte ReadAppBlock = 0x1b;
        public const byte WriteAppBlock = 0x1c;
        public const byte ReadSortBlock = 0x1d;
        public const byte WriteSortBlock = 0x1e;
        public const byte ReadRecord = 0x20;
        public const byte WriteRecord = 0x21;
        public const byte ReadResource = 0x23;
        public const byte WriteResource = 0x24;
        public const byte OpenConduit = 0x2e;
        public const byte EndSync = 0x2f;
        public const byte ReadRecordIdList = 0x31;
    }
}
