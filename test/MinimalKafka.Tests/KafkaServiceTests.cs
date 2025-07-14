using MinimalKafka.Internals;

namespace MinimalKafka.Tests;


public class KafkaServiceTests
{
    private readonly IKafkaBuilder _builder;
    private readonly List<IKafkaProcess> _processes;

    public KafkaServiceTests()
    {
        _builder = Substitute.For<IKafkaBuilder>();
        var _datasource = Substitute.For<IKafkaDataSource>();
        _processes= [
            Substitute.For<IKafkaProcess>(),
            Substitute.For<IKafkaProcess>()
        ];
        _datasource.GetProcesses().Returns(_processes);
        
        _builder.DataSource.Returns(_datasource);
    }

    [Fact]
    public void KafkaService_Processes_ShouldReturnProcessesFromBuilder()
    {
        // Act
        var processes = new KafkaService(_builder).Processes;

        // Assert
        processes.Should().BeEquivalentTo(_processes);
    }

    [Fact]
    public async Task KafkaService_StartAsync_ShouldStartAllProcesses()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var service = new KafkaService(_builder);

        // Act
        await service.StartAsync(cancellationToken);

        await Task.Delay(100);

        // Assert
        foreach (var process in _processes)
        {
            await process.Received(1).Start(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task KafkaService_StopAsync_ShouldStopAllProcesses()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var service = new KafkaService(_builder);

        // Act
        await service.StopAsync(cancellationToken);

        // Assert
        foreach (var process in _processes)
        {
            await process.Received(1).Stop();
        }
    }

        [Fact]
    public void KafkaService_Processes_ShouldReturnEmptyListIfGetProcessesReturnsNull()
    {
        // Arrange
        var builder = Substitute.For<IKafkaBuilder>();
        // Act
        var processes =  new KafkaService(builder).Processes;

        // Assert
        processes.Should().BeEmpty();
    }
}


