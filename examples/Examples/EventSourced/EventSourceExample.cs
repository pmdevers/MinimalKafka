using MinimalKafka.Stream;

namespace Examples.EventSourced;

public static class EventSourceExample
{
    public static void MapEventSourcedExample(this IApplicationBuilder builder)
    {

        builder.MapStream<Guid, Command>("command")
            .Join<Guid, State>("state").OnKey()
            .SplitInto(Processor.Process);


    }
}
