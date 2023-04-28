using Backhand.PalmDb;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class ResourceDbManifest : PalmDbManifest
    {
        public ICollection<ResourceManifest> Resources { get; set; } = Array.Empty<ResourceManifest>();

        public ResourceDbManifest()
        {
        }
        
        public ResourceDbManifest(PalmDbHeader dbHeader, string appInfoPath, string sortInfoPath, IEnumerable<ResourceManifest> resources)
            : base(dbHeader, appInfoPath, sortInfoPath)
        {
            Resources = resources.ToArray();
        }
    }
}