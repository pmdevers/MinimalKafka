using MinimalKafka.Internals;

namespace MinimalKafka.Middlewares.KafkaFileStore;

internal class FileStoreProducerMiddleware(
    IKafkaHydrationService hydrationService
    ) : IKafkaProducerMiddleware
{
    public Task Invoke(ProduceMessage message, KafkaProduceDelegate next)
    {
        hydrationService.DeHydrateAsync(message.Value);

        return next(message);
    }
}
