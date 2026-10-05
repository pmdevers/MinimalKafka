using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middlewares;

namespace MinimalKafka.Internals;

/// <summary>
/// 
/// </summary>
/// <param name="producerMiddlewares"></param>
public class KafkaMiddlewareProducer(
    IReadOnlyList<Func<IServiceProvider, KafkaProducerMiddlewareDelegate>> producerMiddlewares)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Produce(KafkaContext context)
    {
        var producers = new List<Task>();
        var producer = context.RequestServices.GetRequiredService<KafkaProducer>();
        foreach (var message in context.Messages)
        {
            KafkaProduceDelegate next = producer.Invoke;

            for (int i = producerMiddlewares.Count - 1; i >= 0; i--)
            {
                var currentMiddleware = producerMiddlewares[i].Invoke(context.RequestServices);
                KafkaProduceDelegate prevNext = next;
                next = (message) => currentMiddleware(message, prevNext);
            }

            producers.Add(next(message));
        }

        await Task.WhenAll(producers);
    }
}