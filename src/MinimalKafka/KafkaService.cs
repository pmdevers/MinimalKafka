using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : BackgroundService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    private readonly List<Task> _runningTasks = [];


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var process in Processes)
        {
            var task = Task.Run(() => process.Start(stoppingToken), stoppingToken);
            _runningTasks.Add(task);
        }

        await Task.WhenAll(_runningTasks);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var process in Processes)
        {
            await process.Stop();
        }

        await base.StopAsync(cancellationToken);
    }
}
