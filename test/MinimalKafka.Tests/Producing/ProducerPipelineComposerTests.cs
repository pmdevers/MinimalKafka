using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Producing;

namespace MinimalKafka.Tests.Producing;

public class ProducerPipelineComposerTests
{
    [Fact]
    public async Task Compose_WithServices_ShouldExecuteMiddlewareAroundHandler()
    {
        var execution = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IProducerMiddleware>(_ => new TrackingMiddleware(execution, "first"));
        services.AddTransient<IProducerMiddleware>(_ => new TrackingMiddleware(execution, "second"));
        await using var provider = services.BuildServiceProvider();

        ProducerDelegate handler = _ =>
        {
            execution.Add("handler");
            return Task.CompletedTask;
        };

        var pipeline = ProducerPipelineComposer.Compose(handler, provider);

        await pipeline(CreateContext());

        Assert.Equal([
            "first:before",
            "second:before",
            "handler",
            "second:after",
            "first:after"
        ], execution);
    }

    [Fact]
    public async Task Compose_WithMiddleware_ShouldAllowShortCircuit()
    {
        var execution = new List<string>();
        var handlerCalled = false;

        ProducerDelegate handler = _ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new IProducerMiddleware[]
        {
            new ShortCircuitMiddleware(execution),
            new TrackingMiddleware(execution, "second")
        };

        var pipeline = ProducerPipelineComposer.Compose(handler, middleware);

        await pipeline(CreateContext());

        Assert.False(handlerCalled);
        Assert.Equal(["short-circuit"], execution);
    }

    private static ProducerContext CreateContext()
    {
        return new ProducerContext(
            "orders",
            [],
            [],
            new KafkaHeaders(),
            CancellationToken.None);
    }

    private sealed class TrackingMiddleware(List<string> execution, string name) : IProducerMiddleware
    {
        public async Task InvokeAsync(ProducerContext context, ProducerDelegate next)
        {
            execution.Add($"{name}:before");
            await next(context);
            execution.Add($"{name}:after");
        }
    }

    private sealed class ShortCircuitMiddleware(List<string> execution) : IProducerMiddleware
    {
        public Task InvokeAsync(ProducerContext context, ProducerDelegate next)
        {
            execution.Add("short-circuit");
            context.DeliveryResult = new DeliveryResult<byte[], byte[]>();
            return Task.CompletedTask;
        }
    }
}
