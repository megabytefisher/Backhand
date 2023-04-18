using System;

namespace Backhand.Cli.Internal
{
    public class ConsoleTableColumn<T>
    {
        public required string Header { get; init; }
        public int? Width { get; init; } = null;
        public bool IsRightAligned { get; init; } = false;
        public required Func<T, string> GetText { get; init; }
    }
}
