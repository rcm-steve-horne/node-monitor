using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rokos.NodeMonitor
{
    public class CustomConsoleFormatter : ConsoleFormatter
    {
        public CustomConsoleFormatter() : base("customFormatter")
        {
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

            if (message is null)
            {
                return;
            }

            var category = logEntry.Category;
            var periodIndex = category.LastIndexOf(".", StringComparison.Ordinal);

            if (periodIndex != -1)
            {
                category = category.Substring(periodIndex + 1);
            }

            textWriter.WriteLine($"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{logEntry.LogLevel}] {category}: {message}");

            if (logEntry.Exception != null)
            {
                textWriter.WriteLine(logEntry.Exception);
            }
        }
    }
}
