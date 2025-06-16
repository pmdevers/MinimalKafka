using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage;

namespace MinimalKafka.Extension;
public static class AddKafkaBuilderExtensions
{
    public static IAddKafkaBuilder WithInMemoryStore(this IAddKafkaBuilder builder)
    {
        return builder.WithStreamStore(typeof(InMemoryStore<,>));
    }

    public static IAddKafkaBuilder WithStreamStore(this IAddKafkaBuilder builder, Type streamStoreType)
    {
        if (!Array.Exists(streamStoreType.GetInterfaces(),
            x => x.IsGenericType &&
                 x.GetGenericTypeDefinition() == typeof(IStreamStore<,>)
        ))
        {
            throw new InvalidOperationException($"Type: '{streamStoreType}' does not implement IStreamStore<,>");
        }

        builder.Services.AddSingleton(typeof(IStreamStore<,>), streamStoreType);

        return builder;
    }
}
