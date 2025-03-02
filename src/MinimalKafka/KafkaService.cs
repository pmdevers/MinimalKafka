using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : IHostedService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    private readonly List<Task> _runningTasks = [];


    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var process in Processes)
        {
            var task = Task.Run(() => process.Start(cancellationToken), cancellationToken);
            _runningTasks.Add(task);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(Processes.Select(p => p.Stop()));
        await Task.WhenAll(_runningTasks).ConfigureAwait(false);
    }
}
