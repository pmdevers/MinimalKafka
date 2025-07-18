﻿using Microsoft.Extensions.Hosting;

namespace MinimalKafka.Internals;
internal sealed class KafkaService(IKafkaBuilder builder) : BackgroundService
{
    public IEnumerable<IKafkaProcess> Processes
        = builder.DataSource?.GetProcesses() ?? [];

    private readonly List<Task> _runningTasks = [];

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
        foreach (var process in Processes)
        {
            await process.Stop();
        }

        await base.StopAsync(cancellationToken);
    }
}
