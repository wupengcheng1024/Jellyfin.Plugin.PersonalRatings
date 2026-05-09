using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Tests.Helpers;

public sealed class TestLogger<T> : ILogger<T>
{
    public List<TestLogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NoopDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new TestLogEntry
        {
            LogLevel = logLevel,
            Message = formatter(state, exception),
            Exception = exception
        });
    }

    public bool Contains(LogLevel logLevel, string text)
    {
        return Entries.Any(entry =>
            entry.LogLevel == logLevel
            && entry.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

public sealed class TestLogEntry
{
    public Exception? Exception { get; set; }

    public LogLevel LogLevel { get; set; }

    public string Message { get; set; } = string.Empty;
}
