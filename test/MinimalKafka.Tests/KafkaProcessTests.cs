using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka.Tests;
public class KafkaProcessTests
{
    private readonly KafkaConsumer _consumer;
    private readonly KafkaDelegate _handler;
    private readonly KafkaProcessOptions _options;
    private readonly KafkaProcess _kafkaProcess;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KafkaProcessTests()
    {
        
        _consumer = Substitute.For<KafkaConsumer>();
        _handler = Substitute.For<KafkaDelegate>();
        _options = new KafkaProcessOptions { Consumer = _consumer, Delegate = _handler };
        _kafkaProcess = KafkaProcess.Create(_options);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [Fact]
    public void KafkaProcess_Create_ShouldReturnKafkaProcessInstance()
    {
        // Arrange & Act
        var instance = KafkaProcess.Create(_options);

        // Assert
        instance.Should().NotBeNull();
        instance.Should().BeOfType<KafkaProcess>();
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldInvokeSubscribeMethodOnce()
    {
        // Arrange
        var task = Task.Run(() => _kafkaProcess.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        // Assert
        _consumer.Received(1).Subscribe();
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldInvokeHandlerWithValidContext()
    {
        // Arrange
        _consumer.Consume(Arg.Any<KafkaDelegate>(), Arg.Any<CancellationToken>()).Returns(async x =>
        {
            await x.Arg<KafkaDelegate>().Invoke(new TestKafkaContext());
        });

        var task = Task.Run(() => _kafkaProcess.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        // Assert
        await _handler.ReceivedWithAnyArgs().Invoke(Arg.Any<KafkaContext>());
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldLogErrorOnException()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();

        _consumer.Logger.Returns(logger);



        _consumer.Consume(Arg.Any<KafkaDelegate>(), Arg.Any<CancellationToken>()).Returns(x =>
        {
            throw new NotImplementedException();
        });

        var process = KafkaProcess.Create(new()
        {
            Consumer = _consumer,
            Delegate = (c) => Task.CompletedTask
        });

        // Act

        var task = async () => await process.Start(_cancellationTokenSource.Token);

        await task.Should().ThrowAsync<KafkaProcesException>();

        // Assert
        logger.Received(1).UnknownProcessException(new NotImplementedException().Message);
    }

    [Fact]
    public async Task KafkaProcess_Stop_ShouldInvokeCloseMethod()
    {
        // Act
        await _kafkaProcess.Stop();

        // Assert
        _consumer.Received(1).Close();
    }


    public class TestKafkaContext : KafkaContext
    {
        public override object? Key => Guid.NewGuid().ToString();

        public override object? Value => Guid.NewGuid().ToString();

        public override Headers Headers => [];

        public override IServiceProvider RequestServices => EmptyServiceProvider.Instance;

        public override IReadOnlyList<object> MetaData => [];

        public override DateTime Timestamp => TimeProvider.System.GetUtcNow().DateTime;
    }
}
