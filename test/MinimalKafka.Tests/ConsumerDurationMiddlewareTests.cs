using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Middleware;
using MinimalKafka.Middleware.Diagnostics;
using MinimalKafka.Producing;
using System.Collections.Concurrent;

namespace MinimalKafka.Tests;

public class ConsumerDurationMiddlewareTests
{
    [Fact]
    public void UseConsumerDurationLogging_ShouldRegisterMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMinimalKafka(builder => builder.WithConsumerDurationLogging());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var middleware = scope.ServiceProvider.GetServices<IConsumerMiddleware>()
            .OfType<ConsumerDurationMiddleware>()
            .Single();

        Assert.Equal("MinimalKafka.Middleware.Diagnostics.ConsumerDurationMiddleware", middleware.GetType().FullName);
    }

    [Fact]
    public async Task Middleware_ShouldLogTopicAndDuration()
    {
        var sink = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(sink));
        services.AddMinimalKafka(builder => builder.WithConsumerDurationLogging());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var middleware = scope.ServiceProvider.GetServices<IConsumerMiddleware>()
            .OfType<ConsumerDurationMiddleware>()
            .Single();

        await middleware.InvokeAsync(CreateContext("orders"), async _ => await Task.Delay(20));

        var entry = sink.Entries.Single();
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("orders", entry.Message);
        Assert.Contains("Consumed message from", entry.Message);
        Assert.Contains("in", entry.Message);
    }

    private static KafkaContext CreateContext(string topic)
    {
        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = topic,
                Message = new Message<byte[], byte[]>
                {
                    Key = [],
                    Value = [],
                    Headers = []
                }
            },
            Substitute.For<IKafkaProducer>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CollectingLogger(CollectingLoggerProvider provider) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                provider.Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
            }
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
