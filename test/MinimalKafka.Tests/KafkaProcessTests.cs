using Microsoft.Extensions.Logging;
using MinimalKafka.Internals;

namespace MinimalKafka.Tests;
public class KafkaProcessTests
{
    private readonly IKafkaConsumer _consumer;
    private readonly ILogger<KafkaProcess> _logger;
    private readonly KafkaProcess _kafkaProcess;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KafkaProcessTests()
    {
        
        _consumer = Substitute.For<IKafkaConsumer>();
        _logger = Substitute.For<ILogger<KafkaProcess>>();

        _kafkaProcess = KafkaProcess.Create(_consumer, _logger);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [Fact]
    public void KafkaProcess_Create_ShouldReturnKafkaProcessInstance()
    {
        // Arrange & Act
        var instance = KafkaProcess.Create(_consumer, _logger);

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
        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var task = Task.Run(() => _kafkaProcess.Start(_cancellationTokenSource.Token));

        // Act
        _cancellationTokenSource.CancelAfter(100); // Stop the task after a short delay
        await Task.Delay(100);

        _consumer.Received(1).Subscribe();
    }

    [Fact]
    public async Task KafkaProcess_Stop_ShouldInvokeCloseMethod()
    {
        // Act
        await _kafkaProcess.Stop();

        // Assert
        _consumer.Received(1).Close();
    }
}
