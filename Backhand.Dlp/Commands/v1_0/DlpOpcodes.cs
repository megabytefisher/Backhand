namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpOpcodes
    {
        public const byte ReadUserInfo              = 0x10;
        public const byte WriteUserInfo             = 0x11;
        public const byte ReadSysInfo               = 0x12;
        public const byte ReadSysDateTime           = 0x13;
        public const byte WriteSysDateTime          = 0x14;
        public const byte ReadStorageInfo           = 0x15;
        public const byte ReadDbList                = 0x16;
        public const byte OpenDb                    = 0x17;
        public const byte CreateDb                  = 0x18;
        public const byte CloseDb                   = 0x19;
        public const byte DeleteDb                  = 0x1a;
        public const byte ReadAppBlock              = 0x1b;
        public const byte WriteAppBlock             = 0x1c;
        public const byte ReadSortBlock             = 0x1d;
        public const byte WriteSortBlock            = 0x1e;
        public const byte ReadNextModifiedRecord    = 0x1f;
        public const byte ReadRecord                = 0x20;
        public const byte WriteRecord               = 0x21;
        public const byte DeleteRecord              = 0x22;
        public const byte ReadResource              = 0x23;
        public const byte WriteResource             = 0x24;
        public const byte DeleteResource            = 0x25;
        public const byte CleanUpDatabase           = 0x26;
        public const byte ResetSyncFlags            = 0x27;
        public const byte CallApplication           = 0x28;
        public const byte ResetSystem               = 0x29;
        public const byte AddSyncLogEntry           = 0x2a;
        public const byte ReadOpenDbInfo            = 0x2b;
        public const byte MoveCategory              = 0x2c;
        //public const byte ProcessRpc                = 0x2d;
        public const byte OpenConduit               = 0x2e;
        public const byte EndSync                   = 0x2f;
        public const byte ResetRecordIndex          = 0x30;
        public const byte ReadRecordIdList          = 0x31;
    }
}
