using MinimalKafka;

namespace Examples.Features.Resilience;

public static class Retry
{
    private static Random _rnd = new();

    public const string Topic = "retry";

    public static Task Handler(KafkaContext context)
    {
        //var rnd = _rnd.Next(1, 10);
        //if (rnd > 5)
        //{
        throw new InvalidOperationException($"Throwing random exception.");
        //}

        return Task.CompletedTask;
    }
}
