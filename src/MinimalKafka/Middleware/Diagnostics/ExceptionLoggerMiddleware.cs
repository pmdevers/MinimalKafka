using Microsoft.Extensions.Logging;
using System.Text;

namespace MinimalKafka.Middleware.Diagnostics;

internal class ExceptionLoggerMiddleware(ILogger<ExceptionLoggerMiddleware> logger) : IConsumerMiddleware
{
    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var message = Encoding.UTF8.GetString(context.Value ?? []);

            logger.LogError(ex,
                "Error processing message: '{Message}' in topic '{Topic}[{Partition}]' with offset '{Offset}'",
                message,
                context.Topic,
                context.ConsumeResult.Partition,
                context.ConsumeResult.Offset);

            throw new InvalidOperationException($"Error processing message: '{message}' in topic '{context.Topic}[{context.ConsumeResult.Partition}]' with offset '{context.ConsumeResult.Offset}'", ex);
        }
    }
}
