using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using MinimalKafka.Builders;
using MinimalKafka.Helpers;
using MinimalKafka.Internals;
using System;

namespace MinimalKafka.Tests;
public class KafkaProcessTests
{
    private readonly KafkaConsumer _consumer;
    private readonly KafkaDelegate _handler;
    private readonly ILogger<KafkaProcess> _logger;
    private readonly KafkaProcess _kafkaProcess;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KafkaProcessTests()
    {
        
        _consumer = Substitute.For<KafkaConsumer>();
        _handler = Substitute.For<KafkaDelegate>();
        _logger = Substitute.For<ILogger<KafkaProcess>>();

        _kafkaProcess = new KafkaProcess(_consumer, _logger);
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

        // Assert
        await _handler.ReceivedWithAnyArgs().Invoke(Arg.Any<KafkaContext>());
    }

    [Fact]
    public async Task KafkaProcess_Start_ShouldLogErrorOnException()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();

       
        // Act
        var task = async () => await _kafkaProcess.Start(_cancellationTokenSource.Token);

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
}
