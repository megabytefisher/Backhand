using System;
using System.Collections.Generic;
using System.Linq;
using Backhand.PalmDb;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class RecordDbManifest : PalmDbManifest
    {
        public ICollection<RecordManifest> Records { get; set; } = Array.Empty<RecordManifest>();

        public RecordDbManifest()
        {
        }
        
        public RecordDbManifest(PalmDbHeader dbHeader, string appInfoPath, string sortInfoPath, IEnumerable<RecordManifest> records)
            : base(dbHeader, appInfoPath, sortInfoPath)
        {
            Records = records.ToArray();
        }
    }
}