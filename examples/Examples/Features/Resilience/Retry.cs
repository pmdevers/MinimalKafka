using MinimalKafka;

namespace Examples.Features.Resilience;

public static class Retry
{
    public const string Topic = "retry";

    public static Task Handler(KafkaContext context)
    {
        throw new InvalidOperationException($"Throwing random exception.");
    }
}
