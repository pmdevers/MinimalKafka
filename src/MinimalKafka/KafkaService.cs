using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : IHostedService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(Processes, cancellationToken, async (p, t) =>
        {
            await p.Start(t);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var process in Processes)
        {
            process.Stop();
        }

        return Task.CompletedTask;
    }
}
