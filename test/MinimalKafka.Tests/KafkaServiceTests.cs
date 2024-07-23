using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Pmdevers.MinimalKafka;
using Pmdevers.MinimalKafka.Builders;
using Xunit;

namespace MinimalKafka.Tests;


public class KafkaServiceTests
{
    private readonly IKafkaBuilder _builder;
    private readonly KafkaService _service;
    private readonly List<IKafkaProcess> _processes;

    public KafkaServiceTests()
    {
        _builder = Substitute.For<IKafkaBuilder>();
        var _datasource = Substitute.For<IKafkaDataSource>();
        _processes= [
            Substitute.For<IKafkaProcess>(),
            Substitute.For<IKafkaProcess>()
        ];
        _datasource.GetProceses().Returns(_processes);
        
        _builder.DataSource.Returns(_datasource);
        
        _service = new KafkaService(_builder);
    }

    [Fact]
    public void KafkaService_Processes_ShouldReturnProcessesFromBuilder()
    {
        // Act
        var processes = _service.Processes;

        // Assert
        processes.Should().BeEquivalentTo(_processes);
    }

    [Fact]
    public async Task KafkaService_StartAsync_ShouldStartAllProcesses()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _service.StartAsync(cancellationToken);

        // Assert
        foreach (var process in _processes)
        {
            process.Received(1).Start(cancellationToken);
        }
    }

    [Fact]
    public async Task KafkaService_StopAsync_ShouldStopAllProcesses()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _service.StopAsync(cancellationToken);

        // Assert
        foreach (var process in _processes)
        {
            process.Received(1).Stop();
        }
    }

        [Fact]
    public void KafkaService_Processes_ShouldReturnEmptyListIfGetProcessesReturnsNull()
    {
        // Arrange
        _builder.DataSource.GetProceses().Returns([]);

        // Act
        var processes = _service.Processes;

        // Assert
        processes.Should().BeEmpty();
    }
}


