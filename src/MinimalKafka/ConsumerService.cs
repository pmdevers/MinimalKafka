using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinimalKafka;

internal sealed class ConsumerService
{
    private readonly ILogger<ConsumerService> _logger;
    private readonly TopicConsumerBuilder _builder;

    public ConsumerService(ILogger<ConsumerService> logger, TopicConsumerBuilder builder)
    {
        _logger = logger;
        _builder = builder;
    }

    public void StartConsuming(CancellationToken cancellationToken)
    {
        using (var consumer = _builder.Build())
        {
            _logger.LogInformation("Starting consumer for {topicName}.", _builder.Topic);

            consumer.Subscribe(_builder.Topic);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);

                    var parameters = _builder.Handler.GetMethodInfo()
                        .GetParameters().Select(x => GetFromResults(x, consumeResult));


                    _builder.Handler.DynamicInvoke(parameters.ToArray());
                }
            }
            catch (OperationCanceledException)
            {
                // do nothing on cancellation
            }
            finally
            {
                consumer.Close();
            }
        }
    }

    private object GetFromResults(ParameterInfo parameterInfo, ConsumeResult<string, string> consumeResult)
    {
        return string.Empty;
    }
}
