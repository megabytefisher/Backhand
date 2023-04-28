using System;
using System.Collections.Generic;

namespace Backhand.Cli.Internal.DatabaseDisassembly
{
    public class ResourceDbManifest : PalmDbManifest
    {
        public ICollection<ResourceManifest> Resources { get; set; } = Array.Empty<ResourceManifest>();
    }
}