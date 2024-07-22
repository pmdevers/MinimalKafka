using Microsoft.Extensions.Logging;

namespace Pmdevers.MinimalKafka;
public sealed class KafkaLogger(ILogger<KafkaLogger> logger)
{
    public ILogger<KafkaLogger> Logger { get; } = logger;


}
