﻿using Backhand.Protocols.Dlp;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Dlp
{
    public delegate Task DlpSyncFunc(DlpConnection connection, CancellationToken cancellationToken = default);
}
