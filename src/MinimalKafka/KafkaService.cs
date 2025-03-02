using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : IHostedService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(Processes.AsParallel().Select(p => p.Start(cancellationToken)));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(Processes.AsParallel().Select(p => p.Stop()));
    }
}
