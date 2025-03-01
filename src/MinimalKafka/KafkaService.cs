using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : IHostedService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var p in Processes)
        {
            await p.Start(cancellationToken).ConfigureAwait(false);
        }
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
