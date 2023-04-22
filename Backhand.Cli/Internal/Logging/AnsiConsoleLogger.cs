using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Text;

namespace Backhand.Cli.Internal.Logging
{
    internal sealed class AnsiConsoleLogger : ILogger
    {
        private readonly IAnsiConsole _console;
        private readonly string _categoryName;

        private const string WrappedLinePrefix = "  ";

        public AnsiConsoleLogger(IAnsiConsole console, string categoryName)
        {
            _console = console;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            int consoleWidth = _console.Profile.Width;
            if (consoleWidth == 0)
            {
                consoleWidth = 80;
            }
            
            (string levelText, string levelStyle) = logLevel switch
            {
                LogLevel.Trace => ("trc", "grey"),
                LogLevel.Debug => ("dbg", "grey"),
                LogLevel.Information => ("inf", "silver"),
                LogLevel.Warning => ("wrn", "yellow"),
                LogLevel.Error => ("err", "red"),
                LogLevel.Critical => ("crt", "bold maroon"),
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
            };

            string message = $"LOG : {_categoryName} ({levelText}) -> {formatter(state, exception)}";

            // 
            StringBuilder messageBuilder = new();
            for (int i = 0; i < message.Length; )
            {
                int remainingLength = consoleWidth;

                if (i > 0)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.Append(WrappedLinePrefix);
                    remainingLength -= WrappedLinePrefix.Length;
                }

                int length = Math.Min(remainingLength, message.Length - i);
                messageBuilder.Append(message, i, length);
                i += length;
            }
            string wrappedMessage = messageBuilder.ToString();
            
            _console.MarkupLineInterpolated($"[{levelStyle}]{wrappedMessage}[/]");
            if (exception != null)
            {
                _console.WriteException(exception);
            }
        }
    }
}
