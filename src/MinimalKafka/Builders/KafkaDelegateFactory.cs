using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Metadata;
using MinimalKafka.Serializers;
using System.Linq.Expressions;
using System.Reflection;

namespace MinimalKafka.Builders;
internal static class KafkaDelegateFactory
{
    public static KafkaDelegateResult Create(Delegate handler, KafkaDelegateFactoryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var targetExpression = handler.Target switch
        {
            object => Expression.Convert(TargetExpr, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = KafkaDelegateFactoryContext.Create(handler, options);
        var targetableKafkaDelegate = CreateTargetableRequestDelegate(handler.Method, targetExpression, factoryContext);
        var finalKafkaDelegate = targetableKafkaDelegate switch
        {
            null => (KafkaDelegate)handler,
            _ => kafkaContext => targetableKafkaDelegate(handler.Target, kafkaContext),
        };

        return KafkaDelegateResult.Create(finalKafkaDelegate, factoryContext);
    }

    private static Func<object?, KafkaContext, Task>? CreateTargetableRequestDelegate(
        MethodInfo methodInfo,
        Expression? targetExpression,
        KafkaDelegateFactoryContext factoryContext
        )
    {
        factoryContext.ArgumentExpressions ??= CreateArguments(methodInfo.GetParameters(), factoryContext);
        factoryContext.MethodCall = CreateMethodCall(methodInfo, targetExpression, factoryContext);

        Func<object?, KafkaContext, Task> continuation;

        if (factoryContext.MethodCall.Type == typeof(void))
        {
            var block = Expression.Block(
                factoryContext.MethodCall,
                Expression.Constant(Task.CompletedTask)
            );
            continuation = Expression.Lambda<Func<object?, KafkaContext, Task>>(
                block, TargetExpr, KafkaContextExpr).Compile();

        }
        else
        {
            continuation = Expression.Lambda<Func<object?, KafkaContext, Task>>(
                    factoryContext.MethodCall, TargetExpr, KafkaContextExpr).Compile();
        }

        if (factoryContext.Handler is KafkaDelegate)
        {
            return null;
        }

        return async (target, kafkaContext) =>
        {
            await continuation(target, kafkaContext);
        };
    }

    private static MethodCallExpression CreateMethodCall(MethodInfo methodInfo, Expression? target, KafkaDelegateFactoryContext factoryContext)
        => target is null ?
        Expression.Call(methodInfo, factoryContext.ArgumentExpressions) :
        Expression.Call(target, methodInfo, factoryContext.ArgumentExpressions);

    private static Expression[]? CreateArguments(ParameterInfo[] parameters, KafkaDelegateFactoryContext factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return [];
        }

        var arguments = new Expression[parameters.Length];

        factoryContext.ArgumentTypes = new Type[parameters.Length];
        factoryContext.BoxedArgs = new Expression[parameters.Length];
        factoryContext.Parameters = [.. parameters];

        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = CreateArgument(parameters[i], factoryContext);
            factoryContext.ArgumentTypes[i] = parameters[i].ParameterType;
            factoryContext.BoxedArgs[i] = Expression.Convert(arguments[i], typeof(object));
        }

        if (factoryContext.HasInferredBody)
        {
            throw new InvalidOperationException("Method has unresolved parameter.");
        }

        return arguments;
    }

    private static Expression CreateArgument(ParameterInfo parameter, KafkaDelegateFactoryContext factoryContext)
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

            var serializerType = typeof(IKafkaSerializer<>).MakeGenericType(parameter.ParameterType);
            var getSerializerExpr = Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(serializerType), RequestServicesExpr);
            var deserializeMethod = serializerType.GetMethod("Deserialize", [typeof(byte[]), typeof(bool)])!;
            var valueExpr = Expression.Property(KafkaContextExpr, nameof(KafkaContext.Key));


            return Expression.Call(
                getSerializerExpr,
                deserializeMethod,
                valueExpr,
                Expression.Constant(false));

        }
        if (attributes.OfType<IFromValueMetadata>().FirstOrDefault() is { } ||
            parameter.Name.Equals(nameof(KafkaContext.Value), StringComparison.CurrentCultureIgnoreCase))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, KafkaDelegateFactoryConstants.ValueAttribute);
            factoryContext.ValueType = parameter.ParameterType;

            var serializerType = typeof(IKafkaSerializer<>).MakeGenericType(parameter.ParameterType);
            var getSerializerExpr = Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(serializerType), RequestServicesExpr);
            var deserializeMethod = serializerType.GetMethod("Deserialize", [typeof(byte[]), typeof(bool)])!;
            var valueExpr = Expression.Property(KafkaContextExpr, nameof(KafkaContext.Value));


            return Expression.Call(
                getSerializerExpr,
                deserializeMethod,
                valueExpr,
                Expression.Constant(false));
        }

        if (parameter.ParameterType == typeof(KafkaContext))
        {
            return KafkaContextExpr;
        }

        if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService
            && serviceProviderIsService.IsService(parameter.ParameterType))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, KafkaDelegateFactoryConstants.ServiceParameter);
            return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }

        factoryContext.HasInferredBody = true;
        return Expression.Empty();

    }

#pragma warning disable IDE1006 // Naming Styles

    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
    private static readonly ParameterExpression KafkaContextExpr = Expression.Parameter(typeof(KafkaContext), "kafkaContext");

    private static readonly MemberExpression RequestServicesExpr = Expression.Property(KafkaContextExpr,
        typeof(KafkaContext).GetProperty(nameof(KafkaContext.RequestServices))!);

    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions)
        .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService),
        BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider)])!;


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

    public Expression[]? ArgumentExpressions { get; set; }
    public Type[]? ArgumentTypes { get; set; }
    public Expression[]? BoxedArgs { get; set; }

    public Type KeyType { get; set; } = typeof(Ignore);
    public Type ValueType { get; set; } = typeof(Ignore);

    public bool HasInferredBody { get; set; }
    public Expression? MethodCall { get; set; }

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

internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
    public object? GetService(Type serviceType) => null;
}

internal class RfdKafkaBuilder(IServiceProvider serviceProvider) : KafkaBuilder(serviceProvider)
{
}
