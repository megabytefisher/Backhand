using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Concurrent;

namespace Backhand.Cli.Internal.Logging
{
    [ProviderAlias("Console")]
    internal sealed class AnsiConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IAnsiConsole _console;
        private readonly ConcurrentDictionary<string, AnsiConsoleLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        public AnsiConsoleLoggerProvider(IAnsiConsole console)
        {
            _console = console;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new AnsiConsoleLogger(_console, name));

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
