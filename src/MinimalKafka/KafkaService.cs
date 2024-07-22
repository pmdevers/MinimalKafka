using Microsoft.Extensions.Hosting;
using Pmdevers.MinimalKafka.Builders;

namespace Pmdevers.MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : IHostedService
{
    public IEnumerable<IKafkaProcess> Processes
        => builder.DataSource?.GetProceses() ?? [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var process in Processes)
        {
            process.Start(cancellationToken);
        }

        return Task.CompletedTask;
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
