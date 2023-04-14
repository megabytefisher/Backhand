using Backhand.Common.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.FileSerialization
{
    [BinarySerializable]
    public class PdbEntryListHeader
    {
        [BinarySerialize]
        public uint NextListId { get; set; }

        [BinarySerialize]
        public ushort Length { get; set; }
    }
}
