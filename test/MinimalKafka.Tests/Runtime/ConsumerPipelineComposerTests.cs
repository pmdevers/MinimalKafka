using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;

namespace MinimalKafka.Tests.Runtime;

public class ConsumerPipelineComposerTests
{
    [Fact]
    public async Task Compose_WithRegistration_ShouldExecuteGlobalAndTopicMiddlewareAroundHandler()
    {
        var execution = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IConsumerMiddleware>(_ => new TrackingMiddleware(execution, "global"));
        await using var provider = services.BuildServiceProvider();

        var registry = new TopicRegistry();
        var registration = registry.Add("orders", (Func<KafkaContext, Task>)(_ =>
        {
            execution.Add("handler");
            return Task.CompletedTask;
        }));

        registration.Middleware.Add(_ => new TrackingMiddleware(execution, "topic"));

        var pipeline = ConsumerPipelineComposer.Compose(registration, provider);

        await pipeline(CreateContext(provider));

        Assert.Equal(
        [
            "global:before",
            "topic:before",
            "handler",
            "topic:after",
            "global:after"
        ], execution);
    }

    [Fact]
    public async Task Compose_WithRegistration_ShouldAllowGlobalMiddlewareToShortCircuit()
    {
        var execution = new List<string>();
        var handlerInvoked = false;

        var services = new ServiceCollection();
        services.AddTransient<IConsumerMiddleware>(_ => new ShortCircuitMiddleware(execution));
        await using var provider = services.BuildServiceProvider();

        var registry = new TopicRegistry();
        var registration = registry.Add("orders", (Func<KafkaContext, Task>)(_ =>
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        }));
        registration.Middleware.Add(_ => new TrackingMiddleware(execution, "topic"));

        var pipeline = ConsumerPipelineComposer.Compose(registration, provider);

        await pipeline(CreateContext(provider));

        Assert.False(handlerInvoked);
        Assert.Equal(["global:short-circuit"], execution);
    }

    private static KafkaContext CreateContext(IServiceProvider provider)
    {
        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = "orders",
                Message = new Message<byte[], byte[]>
                {
                    Key = [],
                    Value = [],
                    Headers = []
                }
            },
            new NullKafkaProducer(),
            provider,
            CancellationToken.None);
    }

    private sealed class TrackingMiddleware(List<string> execution, string name) : IConsumerMiddleware
    {
        public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
        {
            execution.Add($"{name}:before");
            await next(context);
            execution.Add($"{name}:after");
        }
    }

    private sealed class ShortCircuitMiddleware(List<string> execution) : IConsumerMiddleware
    {
        public Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
        {
            execution.Add("global:short-circuit");
            return Task.CompletedTask;
        }
    }

    private sealed class NullKafkaProducer : IKafkaProducer
    {
        public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
            string topic,
            TKey key,
            TValue value,
            KafkaHeaders? headers = null,
            string? format = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
