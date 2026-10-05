namespace MinimalKafka.Middlewares.KafkaFileStore;

internal class FileStoreConsumerMiddleware(IKafkaHydrationService hydrationService) : IKafkaMiddleware
{
    public async Task InvokeAsync(KafkaContext context, KafkaDelegate next)
    {
        await hydrationService.ReHydrateAsync(context);
        await next(context);
    }
}
