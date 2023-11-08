using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalKafka;
internal class KafkaService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public KafkaService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateAsyncScope();
            var tb = scope.ServiceProvider.GetRequiredService<ITopicConsumerBuilder>();

            await Task.Delay(5, stoppingToken);
        }
    }
}
