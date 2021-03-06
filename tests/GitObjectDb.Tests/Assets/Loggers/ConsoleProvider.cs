using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GitObjectDb.Tests.Assets.Loggers
{
    public sealed class ConsoleProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new ConcurrentDictionary<string, ConsoleLogger>();
        private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public ILogger CreateLogger(string name) =>
            _loggers.GetOrAdd(name, _ => new ConsoleLogger(name, _scopeProvider));

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
        {
        }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
    }
}
