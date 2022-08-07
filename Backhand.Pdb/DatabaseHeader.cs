using Backhand.Pdb.Utility;
using Backhand.Utility.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class DatabaseHeader
    {
        public string Name { get; set; }
        public DatabaseAttributes Attributes { get; set; }
        public ushort Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public uint ModificationNumber { get; set; }
        //public uint AppInfoId { get; set; }
        //public uint SortInfoId { get; set; }
        public string Type { get; set; }
        public string Creator { get; set; }
        public uint UniqueIdSeed { get; set; }
    }
}
