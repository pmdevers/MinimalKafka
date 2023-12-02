using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MinimalKafka;

public sealed class TopicConsumerService : IHostedService
{
    private readonly ITopicConsumerBuilder _builder;
    private readonly IServiceProvider _serviceProvider;

    public TopicConsumerService(ITopicConsumerBuilder builder, IServiceProvider serviceProvider)
    {
        _builder = builder;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var item in _builder.DataSource.GetTopics())
        {
            Task.Run(() => new TopicConsumer(item, _serviceProvider).StartAsync(cancellationToken), cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
