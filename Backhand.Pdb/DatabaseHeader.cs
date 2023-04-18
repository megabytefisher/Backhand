using Backhand.Pdb.FileSerialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Pdb
{
    public class DatabaseHeader : Database
    {
        public override Task SerializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            PdbHeader header = await PdbSerialization.ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            LoadFileHeader(header);
        }
    }
}