using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;
using MinimalKafka.Middlewares.DeadletterQueue;

namespace MinimalKafka.Tests;

public class TestConsumerBuilder(IKafkaConsumer consumer) : IKafkaConsumerBuilder
{
    public IKafkaConsumer Build() => consumer;

    public IKafkaConsumerBuilder WithKey(KafkaConsumerKey key)
    {
        return this;
    }

    public IKafkaConsumerBuilder WithMetadata(IReadOnlyList<object> metadata)
    {
        return this;
    }
}

internal sealed class BlockingResolver : IDeadLetterResolver
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _hasPending;

    public string MarkPending(KafkaContext context)
    {
        _hasPending = true;
        return $"{context.TopicName}:{context.Partition}:{context.Offset}";
    }

    public bool HasPending(KafkaContext context) => _hasPending;

    public Task WaitForResolutionAsync(KafkaContext context, CancellationToken cancellationToken)
        => _tcs.Task.WaitAsync(cancellationToken);

    public bool Resolve(string topic, int partition, long offset)
    {
        _hasPending = false;
        return _tcs.TrySetResult();
    }
}

public class KafkaProcessTests
{
    private readonly IKafkaConsumer _consumer = Substitute.For<IKafkaConsumer>();
    private readonly KafkaProcessBuilder _kafkaProcessBuilder;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IServiceProvider _serviceProvider;

    public KafkaProcessTests()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka(x => x.WithClientId("test-client"));

        _serviceProvider = services.BuildServiceProvider();

        var metadata = ConfigMetadataAttribute.From(null);

        metadata.AddOrUpdate("group.id", "test-group");
        metadata.AddOrUpdate("bootstrap.servers", "localhost:9092");

        _kafkaProcessBuilder = KafkaProcessBuilder.Create(_serviceProvider)
            .WithDelegate(_ => Task.CompletedTask)
            .WithConsumerBuilder(new TestConsumerBuilder(_consumer))
            .WithMetadata([metadata])
            .WithKey(KafkaConsumerKey.Random("test-topic"))
            .WithMiddleware([]);

        _cancellationTokenSource = new CancellationTokenSource();
    }

    [Fact]
    public void KafkaProcess_Create_ShouldReturnKafkaProcessInstance()
    {
        var instance = _kafkaProcessBuilder.Build();

        // Assert
        instance.Should().NotBeNull();
        instance.Should().BeOfType<KafkaProcess>();
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldInvokeSubscribeMethodOnce()
    {
        var _process = _kafkaProcessBuilder.Build();

        // Arrange
        var task = Task.Run(() => _process.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        // Assert
        _consumer.Received(1).Subscribe();
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldInvokeHandlerWithValidContext()
    {
        var _process = _kafkaProcessBuilder.Build();

        // Arrange
        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(KafkaContext.Empty());

        var task = Task.Run(() => _process.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        _consumer.Received(1).Subscribe();
    }

    [Fact]
    public async Task KafkaProcess_Stop_ShouldInvokeCloseMethod()
    {
        var _process = _kafkaProcessBuilder.Build();

        // Act
        await _process.Stop();

        // Assert
        _consumer.Received(1).Close();
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldCommitConsumedContext_WhenNoPendingDlq()
    {
        var message = new KafkaMessage("test-topic", [1], [2], []) with { Partition = 0, Offset = 1 };
        using var consumedContext = KafkaContext.Create(KafkaConsumerKey.Random("test-topic"), [], message, _serviceProvider);

        _consumer.Consume(Arg.Any<CancellationToken>()).Returns(_ => consumedContext, _ => KafkaContext.Empty());

        var process = _kafkaProcessBuilder.Build();

        var run = Task.Run(() => process.Start(_cancellationTokenSource.Token));
        _cancellationTokenSource.CancelAfter(150);
        await Task.Delay(120);

        _consumer.Received().Commit(Arg.Any<KafkaContext>());
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldWaitForDlqResolution_BeforeCommit()
    {
        var services = new ServiceCollection();
        services.AddMinimalKafka(x => x.WithClientId("test-client"));
        var resolver = new BlockingResolver();
        services.AddSingleton<IDeadLetterResolver>(resolver);
        var sp = services.BuildServiceProvider();

        var metadata = ConfigMetadataAttribute.From(null);
        metadata.AddOrUpdate("group.id", "test-group");
        metadata.AddOrUpdate("bootstrap.servers", "localhost:9092");

        var consumer = Substitute.For<IKafkaConsumer>();
        var message = new KafkaMessage("test-topic", [1], [2], []) with { Partition = 0, Offset = 2 };
        using var consumedContext = KafkaContext.Create(KafkaConsumerKey.Random("test-topic"), [], message, sp);

        resolver.MarkPending(consumedContext);

        consumer.Consume(Arg.Any<CancellationToken>()).Returns(_ => consumedContext, _ => KafkaContext.Empty());

        var processBuilder = KafkaProcessBuilder.Create(sp)
            .WithDelegate(_ => Task.CompletedTask)
            .WithConsumerBuilder(new TestConsumerBuilder(consumer))
            .WithMetadata([metadata])
            .WithKey(KafkaConsumerKey.Random("test-topic"))
            .WithMiddleware([]);

        var process = processBuilder.Build();

        var commitSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.When(x => x.Commit(Arg.Any<KafkaContext>())).Do(_ => commitSignal.TrySetResult());

        var run = Task.Run(() => process.Start(_cancellationTokenSource.Token));

        await Task.Delay(100);

        commitSignal.Task.IsCompleted.Should().BeFalse();

        resolver.Resolve(consumedContext.TopicName, consumedContext.Partition, consumedContext.Offset);

        await commitSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        consumer.Received().Commit(Arg.Any<KafkaContext>());

        _cancellationTokenSource.Cancel();
        await run.ContinueWith(_ => Task.CompletedTask);
    }
}
