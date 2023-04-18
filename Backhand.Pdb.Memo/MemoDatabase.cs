namespace Backhand.Pdb.Memo
{
    public class MemoDatabase : RecordDatabase<MemoRecord>
    {
        public MemoDatabase()
        {
            Name = "MemoDB";
            Type = "DATA";
            Creator = "memo";
        }
    }
}
