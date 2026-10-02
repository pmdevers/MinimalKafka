using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Metadata;
using MinimalKafka.Serializers;
using System.Collections.Concurrent;
using System.Reflection;

namespace MinimalKafka.Builders;
internal static class KafkaDelegateFactory
{
    public static KafkaDelegateResult Create(Delegate handler, KafkaDelegateFactoryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var factoryContext = KafkaDelegateFactoryContext.Create(handler, options);
        var targetableKafkaDelegate = CreateTargetableRequestDelegate(handler.Method, factoryContext);
        var finalKafkaDelegate = targetableKafkaDelegate switch
        {
            null => (KafkaDelegate)handler,
            _ => kafkaContext => targetableKafkaDelegate(handler.Target, kafkaContext),
        };

        return KafkaDelegateResult.Create(finalKafkaDelegate, factoryContext);
    }

    private static Func<object?, KafkaContext, Task>? CreateTargetableRequestDelegate(
        MethodInfo methodInfo,
        KafkaDelegateFactoryContext factoryContext)
    {
        var parameterBinders = CreateArgumentBinders(methodInfo.GetParameters(), factoryContext);

        if (factoryContext.Handler is KafkaDelegate)
        {
            return null;
        }

        return async (target, kafkaContext) =>
        {
            var arguments = await BindArgumentsAsync(parameterBinders, kafkaContext);
            var result = methodInfo.Invoke(target, arguments);

            if (result is Task task)
            {
                await task;
            }
        };
    }

    private static KafkaParameterBinder[] CreateArgumentBinders(ParameterInfo[] parameters, KafkaDelegateFactoryContext factoryContext)
    {
        if (parameters.Length == 0)
        {
            return [];
        }

        var binders = new KafkaParameterBinder[parameters.Length];
        factoryContext.Parameters = [.. parameters];

        for (var i = 0; i < parameters.Length; i++)
        {
            binders[i] = CreateArgumentBinder(parameters[i], factoryContext);
        }

        if (factoryContext.HasInferredBody)
        {
            throw new InvalidOperationException("Method has unresolved parameter.");
        }

        return binders;
    }

    private static KafkaParameterBinder CreateArgumentBinder(ParameterInfo parameter, KafkaDelegateFactoryContext factoryContext)
    {
        if (parameter.Name is null)
        {
            throw new InvalidOperationException();
        }

        if (parameter.ParameterType.IsByRef)
        {
            var attribute = "ref";

            if (parameter.Attributes.HasFlag(ParameterAttributes.In))
            {
                attribute = "in";
            }
            else if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            {
                attribute = "out";
            }

            throw new NotSupportedException($"The by reference parameter '{attribute} {parameter.Name}' is not supported.");
        }

        var attributes = parameter.GetCustomAttributes();

        if (attributes.OfType<IFromKeyMetadata>().FirstOrDefault() is { } ||
            parameter.Name.Equals(nameof(KafkaContext.Key), StringComparison.CurrentCultureIgnoreCase))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, KafkaDelegateFactoryConstants.KeyAttribute);
            factoryContext.KeyType = parameter.ParameterType;
            return kafkaContext => DeserializeAndReHydrateAsync(parameter.ParameterType, kafkaContext.RequestServices, kafkaContext.Key.ToArray());
        }

        if (attributes.OfType<IFromValueMetadata>().FirstOrDefault() is { } ||
            parameter.Name.Equals(nameof(KafkaContext.Value), StringComparison.CurrentCultureIgnoreCase))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, KafkaDelegateFactoryConstants.ValueAttribute);
            factoryContext.ValueType = parameter.ParameterType;
            return kafkaContext => DeserializeAndReHydrateAsync(parameter.ParameterType, kafkaContext.RequestServices, kafkaContext.Value.ToArray());
        }

        if (parameter.ParameterType == typeof(KafkaContext))
        {
            return kafkaContext => ValueTask.FromResult<object?>(kafkaContext);
        }

        if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService
            && serviceProviderIsService.IsService(parameter.ParameterType))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, KafkaDelegateFactoryConstants.ServiceParameter);
            return kafkaContext => ValueTask.FromResult((object?)kafkaContext.RequestServices.GetRequiredService(parameter.ParameterType));
        }

        factoryContext.HasInferredBody = true;
        throw new InvalidOperationException($"Unable to resolve service for parameter '{parameter.Name}' of type '{parameter.ParameterType.FullName}'. Register the service in the container.");
    }

    public static async ValueTask<object?> DeserializeAndReHydrateAsync<T>(IServiceProvider serviceProvider, ReadOnlyMemory<byte> value)
    {
        var serializer = serviceProvider.GetRequiredService<IKafkaSerializer<T>>();
        var result = serializer.Deserialize(value.Span);

        if (result is not null && serviceProvider.GetService<IKafkaHydrationService>() is { } hydrationService)
        {
            await hydrationService.ReHydrateAsync(result);
        }

        return result;
    }

    private static async ValueTask<object?[]> BindArgumentsAsync(KafkaParameterBinder[] parameterBinders, KafkaContext kafkaContext)
    {
        var arguments = new object?[parameterBinders.Length];

        for (var i = 0; i < parameterBinders.Length; i++)
        {
            arguments[i] = await parameterBinders[i](kafkaContext);
        }

        return arguments;
    }

    private static ValueTask<object?> DeserializeAndReHydrateAsync(Type parameterType, IServiceProvider serviceProvider, ReadOnlyMemory<byte> value)
        => _deserializerCache.GetOrAdd(parameterType, static type =>
            (Func<IServiceProvider, ReadOnlyMemory<byte>, ValueTask<object?>>)DeserializeAndReHydrateAsyncMethod
                .MakeGenericMethod(type)
                .CreateDelegate(typeof(Func<IServiceProvider, ReadOnlyMemory<byte>, ValueTask<object?>>)))(serviceProvider, value);

    private delegate ValueTask<object?> KafkaParameterBinder(KafkaContext kafkaContext);

#pragma warning disable IDE1006 // Naming Styles

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, ReadOnlyMemory<byte>, ValueTask<object?>>> _deserializerCache = new();

    private static readonly MethodInfo DeserializeAndReHydrateAsyncMethod = typeof(KafkaDelegateFactory)
        .GetMethod(nameof(DeserializeAndReHydrateAsync), BindingFlags.Public | BindingFlags.Static)!
        .GetGenericMethodDefinition();

#pragma warning restore IDE1006 // Naming Styles
}

internal static class KafkaDelegateFactoryConstants
{
    public const string KeyAttribute = "Key (Attribute)";
    public const string ValueAttribute = "Value (Attribute)";
    public const string ServiceParameter = "Services (Inferred)";
}

internal sealed class KafkaDelegateResult
{
    private KafkaDelegateResult(KafkaDelegate kafkaDelegate, Type keyType, Type valueType, IReadOnlyList<object> metadata)
    {
        Delegate = kafkaDelegate;
        KeyType = keyType;
        ValueType = valueType;
        Metadata = metadata;
    }

    public static KafkaDelegateResult Create(KafkaDelegate kafkaDelegate, KafkaDelegateFactoryContext context)
    {
        return new(kafkaDelegate, context.KeyType, context.ValueType, context.KafkaBuilder.MetaData);
    }

    public KafkaDelegate Delegate { get; }
    public Type KeyType { get; }
    public Type ValueType { get; }
    public IReadOnlyList<object> Metadata { get; }
}

internal class KafkaDelegateFactoryOptions
{
    public required IServiceProvider? ServiceProvider { get; init; }
    public required IKafkaBuilder KafkaBuilder { get; init; }
}

internal class KafkaDelegateFactoryContext
{
    public required IServiceProvider ServiceProvider { get; init; }
    public required IServiceProviderIsService? ServiceProviderIsService { get; init; }
    public required IKafkaBuilder KafkaBuilder { get; init; }
    public Delegate? Handler { get; set; }
    public Dictionary<string, string> TrackedParameters { get; } = [];
    public List<ParameterInfo> Parameters { get; set; } = [];

    public Type KeyType { get; set; } = typeof(Ignore);
    public Type ValueType { get; set; } = typeof(Ignore);

    public bool HasInferredBody { get; set; }

    public static KafkaDelegateFactoryContext Create(Delegate? handler, KafkaDelegateFactoryOptions? options)
    {
        var serviceProvider = options?.ServiceProvider ?? EmptyServiceProvider.Instance;
        var kafkabuilder = options?.KafkaBuilder ?? new RfdKafkaBuilder(serviceProvider);

        return new KafkaDelegateFactoryContext()
        {
            ServiceProvider = serviceProvider,
            ServiceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>(),
            KafkaBuilder = kafkabuilder,
            Handler = handler,
        };
    }
}

internal sealed class EmptyServiceProvider : IServiceProvider, IServiceScopeFactory
{
    public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceScopeFactory))
        {
            return this;
        }
        if (serviceType == typeof(IServiceProvider))
        {
            return this;
        }
        return null;
    }

    public IServiceScope CreateScope()
    {
        return new EmptyServiceScope(this);
    }
}

internal sealed class EmptyServiceScope(IServiceProvider serviceProvider) : IServiceScope
{
    public IServiceProvider ServiceProvider => serviceProvider;
    public void Dispose() { }
}

internal class RfdKafkaBuilder(IServiceProvider serviceProvider) : KafkaBuilder(serviceProvider)
{
}
