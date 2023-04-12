using Backhand.Common.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.FileSerialization
{
    public class DatabaseFileEntryListHeader
    {
        [BinarySerialized]
        public uint NextListId { get; set; }

        [BinarySerialized]
        public ushort Length { get; set; }
    }
}
