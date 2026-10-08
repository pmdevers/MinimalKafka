using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;

namespace MinimalKafka.Tests;

public class TopicBuilderTests
{
    [Fact]
    public void Topic_ShouldReturnMappedTopicName()
    {
        var registry = new TopicRegistry();
        ConsumerDelegate handler = _ => Task.CompletedTask;
        var registration = registry.Add("orders", handler);
        var builder = new TopicBuilder(registration);

        Assert.Equal("orders", builder.Topic);
    }

    [Fact]
    public void UseFormat_ShouldSetRegistrationFormat()
    {
        var registry = new TopicRegistry();
        ConsumerDelegate handler = _ => Task.CompletedTask;
        var registration = registry.Add("orders", handler);
        var builder = new TopicBuilder(registration);

        builder.UseFormat("avro");

        Assert.Equal("avro", registration.Format);
    }

    [Fact]
    public async Task Use_ShouldAddDelegateMiddleware()
    {
        var registry = new TopicRegistry();
        ConsumerDelegate handler = _ => Task.CompletedTask;
        var registration = registry.Add("orders", handler);
        var builder = new TopicBuilder(registration);
        var nextCalled = false;
        var middlewareCalled = false;

        builder.Use(async (_, next) =>
        {
            middlewareCalled = true;
            await next(CreateContext());
        });

        var services = new ServiceCollection().BuildServiceProvider();
        var middleware = registration.Middleware.Single()(services);

        await middleware.InvokeAsync(CreateContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(middlewareCalled);
        Assert.True(nextCalled);
    }

    [Fact]
    public void UseMiddlewareType_ShouldResolveConstructorArguments()
    {
        var services = new ServiceCollection();
        var state = new MiddlewareState();
        services.AddSingleton(state);
        var provider = services.BuildServiceProvider();

        var registry = new TopicRegistry();
        ConsumerDelegate handler = _ => Task.CompletedTask;
        var registration = registry.Add("orders", handler);
        var builder = new TopicBuilder(registration);

        builder.Use<TestConsumerMiddleware>("prefix");

        _ = registration.Middleware.Single()(provider);

        Assert.Equal("prefix", state.Prefix);
    }

    private static KafkaContext CreateContext()
    {
        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = "orders",
                Message = new Message<byte[], byte[]>
                {
                    Key = [],
                    Value = [],
                    Headers = new Headers()
                }
            },
            Substitute.For<IKafkaProducer>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }

    private sealed class MiddlewareState
    {
        public string? Prefix { get; set; }
    }

    private sealed class TestConsumerMiddleware : IConsumerMiddleware
    {
        public TestConsumerMiddleware(MiddlewareState state, string prefix)
        {
            state.Prefix = prefix;
        }

        public Task InvokeAsync(KafkaContext context, ConsumerDelegate next) => Task.CompletedTask;
    }
}
