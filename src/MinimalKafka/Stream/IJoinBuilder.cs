using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

public interface IJoinBuilder<V1, V2>
{
    IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on);
}


