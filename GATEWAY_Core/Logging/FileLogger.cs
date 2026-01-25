using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Core.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
    private readonly Thread _worker;
    private readonly StreamWriter _writer;
    private readonly LogLevel _minimumLevel;
    private bool _disposed;

    public FileLoggerProvider(string filePath, LogLevel minimumLevel)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        _writer = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        _minimumLevel = minimumLevel;
        _worker = new Thread(Consume) { IsBackground = true, Name = "FileLogger" };
        _worker.Start();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this, _minimumLevel);

    internal void Enqueue(string message)
    {
        if (!_disposed)
        {
            _queue.Add(message);
        }
    }

    private void Consume()
    {
        foreach (var message in _queue.GetConsumingEnumerable())
        {
            _writer.WriteLine(message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        _worker.Join(TimeSpan.FromSeconds(2));
        _writer.Dispose();
        _queue.Dispose();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerProvider _provider;
        private readonly LogLevel _minimumLevel;

        public FileLogger(string categoryName, FileLoggerProvider provider, LogLevel minimumLevel)
        {
            _categoryName = categoryName;
            _provider = provider;
            _minimumLevel = minimumLevel;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var line = $"{DateTimeOffset.Now:O}\t{logLevel}\t{_categoryName}\t{message}";
            if (exception != null)
            {
                line += $"\n{exception}";
            }
            _provider.Enqueue(line);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
