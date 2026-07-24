using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MinimalKafka.Internals;
using MinimalKafka.Middlewares.DeadletterQueue;
using MinimalKafka.Serializers;
using System.Text.Json;

namespace MinimalKafka.Tests;

public class DeadLetterQueueMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldEnqueueDeadLetterMessage_WhenNextThrows()
    {
        var services = new ServiceCollection();
        services.AddSingleton(JsonSerializerOptions.Default);
        services.AddSingleton<ISerializerFactory, SystemTextJsonSerializerFactory>();
        services.AddTransient(typeof(IKafkaSerializer<>), typeof(KafkaSerializerProxy<>));

        var serviceProvider = services.BuildServiceProvider();
        using var context = KafkaContext.Create(
            KafkaConsumerKey.Random("orders"),
            [],
            new KafkaMessage("orders", [1, 2, 3], [4, 5, 6], []) with { Partition = 1, Offset = 9 },
            serviceProvider);

        var resolver = new InMemoryDeadLetterResolver();
        var options = Options.Create(new DeadLetterQueueOptions());
        var middleware = new DeadLetterQueueMiddleware(resolver, options, NullLogger<DeadLetterQueueMiddleware>.Instance);

        await middleware.InvokeAsync(context, _ => throw new KafkaProcesException(new InvalidOperationException("boom"), "wrapped"));

        context.Messages.Should().HaveCount(1);
        resolver.HasPending(context).Should().BeTrue();

        var dlqMessage = context.Messages[0];
        dlqMessage.Topic.Should().Be("orders.dlq");
        dlqMessage.Key.Should().Equal([1, 2, 3]);
        dlqMessage.Value.Should().Equal([4, 5, 6]);
        dlqMessage.Headers.Should().ContainKey("dlq.source.topic");
        dlqMessage.Headers["dlq.source.topic"].Should().Be("orders");
        dlqMessage.Headers.Should().ContainKey("dlq.source.group");
        dlqMessage.Headers["dlq.source.group"].Should().Be(context.GroupId);
        dlqMessage.Headers.Should().ContainKey("dlq.exception.type");
        dlqMessage.Headers["dlq.exception.type"].Should().Contain("InvalidOperationException");
        dlqMessage.Headers.Should().ContainKey("dlq.exception.message");
        dlqMessage.Headers["dlq.exception.message"].Should().Be("boom");
        dlqMessage.Headers.Should().ContainKey("dlq.resolution.key");
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotEnqueueDeadLetterMessage_WhenNextSucceeds()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var context = KafkaContext.Create(
            KafkaConsumerKey.Random("orders"),
            [],
            new KafkaMessage("orders", [1], [2], []),
            serviceProvider);

        var resolver = new InMemoryDeadLetterResolver();
        var options = Options.Create(new DeadLetterQueueOptions());
        var middleware = new DeadLetterQueueMiddleware(resolver, options, NullLogger<DeadLetterQueueMiddleware>.Instance);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Messages.Should().BeEmpty();
        resolver.HasPending(context).Should().BeFalse();
    }

    [Fact]
    public async Task Resolver_ShouldWaitUntilResolved()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var context = KafkaContext.Create(
            KafkaConsumerKey.Random("orders"),
            [],
            new KafkaMessage("orders", [1], [2], []) with { Partition = 2, Offset = 42 },
            serviceProvider);

        var resolver = new InMemoryDeadLetterResolver();
        resolver.MarkPending(context);

        var waitTask = resolver.WaitForResolutionAsync(context, CancellationToken.None);
        waitTask.IsCompleted.Should().BeFalse();

        resolver.Resolve(context.TopicName, context.Partition, context.Offset).Should().BeTrue();
        await waitTask;

        resolver.HasPending(context).Should().BeFalse();
    }
}
