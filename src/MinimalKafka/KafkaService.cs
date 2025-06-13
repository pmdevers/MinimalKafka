using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;

namespace MinimalKafka;
internal class KafkaService(IKafkaBuilder builder) : BackgroundService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProceses() ?? [];

    private readonly List<Task> _runningTasks = [];
    private bool _running = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        foreach (var process in Processes)
        { 
            var task = Task.Run(async () =>
            {
                try
                {
                    await process.Start(cts.Token);
                }
                catch (KafkaProcesException)
                {
                    _running = false;
                    await cts.CancelAsync();
                    throw;
                }
                
            }
            , cts.Token);
            _runningTasks.Add(task);
        } 
        
        await Task.WhenAll(_runningTasks);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_running) {
            return;
        }


        foreach (var process in Processes)
        {
            await process.Stop();
        }

        await base.StopAsync(cancellationToken);
    }
}
