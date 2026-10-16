using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.Basic;

public static class BasicProduce
{
    public const string Route = "/produce";

    public static async Task<IResult> Handle([FromServices] IKafkaProducer producer)
    {
        await producer.ProduceAsync(BasicSubscribe.Topic, 1, "hello");

        return TypedResults.Ok();
    }
}
