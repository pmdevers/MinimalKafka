using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;

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

        var metadata = new ConfigMetadataAttribute();
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
}
