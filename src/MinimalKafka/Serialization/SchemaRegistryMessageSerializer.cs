using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MinimalKafka.Serialization;

/// <summary>Adapts Confluent's generic, schema registry based serializers to runtime types.</summary>
internal abstract class SchemaRegistryMessageSerializer : IMessageSerializer
{
    private readonly ConcurrentDictionary<Type, IKafkaSerializer> _adapters = new();

    public abstract string Format { get; }

    protected abstract string TypeRequirement { get; }

    protected abstract IKafkaSerializer CreateAdapter(Type type);

    public Task<byte[]> SerializeAsync(object value, string topic, Headers headers, CancellationToken cancellationToken) =>
        AdapterFor(value.GetType()).SerializeAsync(value, new SerializationContext(MessageComponentType.Value, topic, headers));

    public Task<object?> DeserializeAsync(byte[] data, Type type, string topic, Headers headers, CancellationToken cancellationToken) =>
        AdapterFor(type).DeserializeAsync(data, new SerializationContext(MessageComponentType.Value, topic, headers));

    protected IKafkaSerializer Build(string methodName, Type type)
    {
        try
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            return (IKafkaSerializer)method.MakeGenericMethod(type).Invoke(this, null)!;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Type '{type.FullName}' cannot be used with the {Format} format: {TypeRequirement}", exception);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private IKafkaSerializer AdapterFor(Type type) => _adapters.GetOrAdd(type, CreateAdapter);

    protected interface IKafkaSerializer
    {
        Task<byte[]> SerializeAsync(object value, SerializationContext context);
        Task<object?> DeserializeAsync(byte[] data, SerializationContext context);
    }

    protected sealed class KafkaSerializer<T>(IAsyncSerializer<T> serializer, IAsyncDeserializer<T> deserializer) : IKafkaSerializer
    {
        public Task<byte[]> SerializeAsync(object value, SerializationContext context) =>
            serializer.SerializeAsync((T)value, context);

        public async Task<object?> DeserializeAsync(byte[] data, SerializationContext context) =>
            await deserializer.DeserializeAsync(data, isNull: false, context).ConfigureAwait(false);
    }
}
