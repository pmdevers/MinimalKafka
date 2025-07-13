using System.Text.Json;

namespace MinimalKafka.Serializers;

internal class SystemTextJsonSerializerFactory(JsonSerializerOptions? options = null) : ISerializerFactory
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public IKafkaSerializer<T> Create<T>()
    {
        return new SystemTextJsonSerializer<T>(_options);
    }
}
