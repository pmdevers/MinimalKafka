using Confluent.Kafka;
using Microsoft.Extensions.Logging;

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
        var validContext = new TestKafkaContext();
        var delay = false;
        _consumer.Consume(Arg.Any<CancellationToken>()).Returns(x =>
        {
            if (delay)
            {
                Task.Delay(200).GetAwaiter().GetResult();
                return KafkaContext.Empty;
            }
            delay = true;
            return validContext;
        }) ;

            
        var task = Task.Run(() => _kafkaProcess.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        // Assert
        await _handler.Received(1).Invoke(validContext);
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldLogErrorOnEmptyContext()
    {
        // Arrange
        var delay = false;
        _consumer.Consume(Arg.Any<CancellationToken>()).Returns(x =>
        {
            if (delay)
            {
                Task.Delay(200).GetAwaiter().GetResult();
                return KafkaContext.Empty;
            }
            delay = true;
            return KafkaContext.Empty;
        });

        var task = Task.Run(() => _kafkaProcess.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        // Assert
        _consumer.Logger.Received(1).LogError("Empty Context!");
        await _handler.DidNotReceive().Invoke(Arg.Any<KafkaContext>());
    }

    [Fact]
    public void KafkaProcess_Stop_ShouldInvokeCloseMethod()
    {
        // Act
        _kafkaProcess.Stop();

        // Assert
        _consumer.Received(1).Close();
    }


    public class TestKafkaContext : KafkaContext
    {
        public override object? Key => Guid.NewGuid().ToString();

        public override object? Value => Guid.NewGuid().ToString();

        public override Headers Headers => [];

        public override IServiceProvider RequestServices => EmptyServiceProvider.Instance;
    }
}
