using MinimalKafka.Attributes;
using MinimalKafka.Producing;

namespace Examples.Features.BasicSubscribe;

public static class BasicSubscribe
{
    public const string Topic = "my-topic";

    public static async Task Handle([FromServices] IKafkaProducer producer, [FromKey] int key, [FromValue] string value)
    {
        await producer.ProduceAsync("other-topic", key, value);
    }
}
