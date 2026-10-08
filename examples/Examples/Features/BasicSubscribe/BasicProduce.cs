using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.BasicSubscribe;

public static class BasicProduce
{
    public static string Route = "/produce";

    public static async Task<IResult> Handle([FromServices] IKafkaProducer producer)
    {
        await producer.ProduceAsync(BasicSubscribe.Topic, 1, "hello");

        return TypedResults.Ok();
    }
}
