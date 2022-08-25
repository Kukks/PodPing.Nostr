using Microsoft.Extensions.Logging;

namespace PodPing.NostrListener;

public class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        Console.WriteLine(formatter.Invoke(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new JunkDisposable();
    }

    public class JunkDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}