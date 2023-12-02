using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka.Factory;

public static class TopicDelegateFactory
{
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly PropertyInfo KeyProperty = typeof(KafkaContext).GetProperty(nameof(KafkaContext.Key))!;
    private static readonly PropertyInfo ValueProperty = typeof(KafkaContext).GetProperty(nameof(KafkaContext.Value))!;

    private static readonly ParameterExpression KafkaContextExpr = ParameterBindingMethodCache.KafkaContextExpr;
    private static readonly MemberExpression RequestServicesExpr = Expression.Property(KafkaContextExpr, typeof(KafkaContext).GetProperty(nameof(KafkaContext.RequestServices))!);

    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");

    public static TopicDelegateMetadataResult InferMetaData(
        MethodInfo methodInfo,
        TopicDelegateFactoryOptions? options = null)
    {
        var factoryContext = CreateFactoryContext(options);

        factoryContext.ArgumentExpressions = CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        return new TopicDelegateMetadataResult
        {
            TopicKeyType = factoryContext.KeyType,
            TopicValueType = factoryContext.ValueType,
            TopicMetadata = AsReadOnlyList(factoryContext.TopicBuilder.Metadata),
        };
    }

    public static TopicDelegateResult Create(Delegate handler, TopicDelegateFactoryOptions? options = null, TopicDelegateMetadataResult? metadataResult = null)
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        var targetExpression = handler.Target switch
        {
            object => Expression.Convert(TargetExpr, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = CreateFactoryContext(options, metadataResult, handler);

        Expression<Func<KafkaContext, object?>> targetFactory = (kafkaContext) => handler.Target;

        var targetableTopicDelegate = CreateTargetableTopicDelegate(handler.Method, targetExpression, factoryContext, targetFactory);

        TopicDelegate finalTopicDelegate = targetableTopicDelegate switch
        {
            null => (TopicDelegate)handler,
            _ => kafkaContext => targetableTopicDelegate(handler.Target, kafkaContext),
        };

        return CreateTopicDelegateResult(finalTopicDelegate, factoryContext);
    }

    private static TopicDelegateResult CreateTopicDelegateResult(TopicDelegate finalTopicDelegate, TopicDelegateFactoryContext factoryContext)
    {
        return new TopicDelegateResult(finalTopicDelegate, null, factoryContext.KeyType, factoryContext.ValueType);
    }

    private static Func<object?, KafkaContext, Task>? CreateTargetableTopicDelegate(
        MethodInfo method,
        Expression? targetExpression,
        TopicDelegateFactoryContext factoryContext,
        Expression<Func<KafkaContext, object?>>? targetFactory)
    {
        factoryContext.ArgumentExpressions ??= CreateArgumentsAndInferMetadata(method, factoryContext);
        factoryContext.MethodCall = CreateMethodCall(method, targetExpression, factoryContext.ArgumentExpressions);

        var continuation = Expression.Lambda<Func<object?, KafkaContext, Task>>(
                    factoryContext.MethodCall, TargetExpr, KafkaContextExpr).Compile();

        var returnType = method.ReturnType;

        if (factoryContext.Handler is TopicDelegate)
        {
            return null;
        }

        return async (target, kafkaContext) =>
        {
            await continuation(target, kafkaContext);
        };

    }

    private static Expression CreateMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments) =>
        target is null ?
        Expression.Call(methodInfo, arguments) :
        Expression.Call(target, methodInfo, arguments);

    private static Expression[] CreateArgumentsAndInferMetadata(MethodInfo methodInfo, TopicDelegateFactoryContext factoryContext)
    {
        var args = CreateArguments(methodInfo.GetParameters(), factoryContext);

        return args;
    }

    private static Expression[] CreateArguments(
        ParameterInfo[]? parameters,
        TopicDelegateFactoryContext factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return Array.Empty<Expression>();
        }

        var args = new Expression[parameters.Length];

        factoryContext.ArgumentTypes = new Type[parameters.Length];
        factoryContext.BoxedArgs = new Expression[parameters.Length];
        factoryContext.Parameters = new List<ParameterInfo>(parameters);

        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = CreateArgument(parameters[i], factoryContext);

            factoryContext.ArgumentTypes[i] = parameters[i].ParameterType;
            factoryContext.BoxedArgs[i] = Expression.Convert(args[i], typeof(object));
        }

        return args;
    }

    private static Expression CreateArgument(ParameterInfo parameter, TopicDelegateFactoryContext factoryContext)
    {
        if (parameter.Name is null)
        {
            throw new InvalidOperationException("Parameter name can not be null");
        }

        var parameterCustomAttributes = parameter.GetCustomAttributes();

        if (parameterCustomAttributes.OfType<IFromKeyMetadata>().FirstOrDefault() is { } keyAttribute || 
            parameter.Name.Equals(nameof(KafkaContext.Key), StringComparison.CurrentCultureIgnoreCase))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.KeyAttribute);

            return BindParameterFromKey(parameter, false, factoryContext);
        }
        if (parameterCustomAttributes.OfType<IFromValueMetadata>().FirstOrDefault() is { } valueAttribute ||
            parameter.Name.Equals(nameof(KafkaContext.Value), StringComparison.CurrentCultureIgnoreCase))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.ValueAttribute);

            return BindParameterFromValue(parameter, false, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.HeaderAttribute);

            return Expression.Empty();
        }
        else if (parameterCustomAttributes.OfType<IFromServiceMetadata>().FirstOrDefault() is { } serviceAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.ServiceAttribute);
            return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }
        else if (parameter.ParameterType == typeof(KafkaContext))
        {
            return KafkaContextExpr;
        }
        else
        {
            if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService &&
                serviceProviderIsService.IsService(parameter.ParameterType))
            {
                factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.ServiceParameter);
                return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
            }

            return Expression.Empty();
        }
    }

    private static Expression BindParameterFromKey(ParameterInfo parameter, bool allowEmpty, TopicDelegateFactoryContext factoryContext)
    {
        factoryContext.KeyType = parameter.ParameterType;
        return Expression.Convert(Expression.Property(KafkaContextExpr, KeyProperty), parameter.ParameterType);
    }

    private static Expression BindParameterFromValue(ParameterInfo parameter, bool allowEmpty, TopicDelegateFactoryContext factoryContext)
    {
        factoryContext.ValueType = parameter.ParameterType;
        return Expression.Convert(Expression.Property(KafkaContextExpr, ValueProperty), parameter.ParameterType);
    }

    //private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, PropertyInfo itemProperty, string key, RequestDelegateFactoryContext factoryContext, string source) =>
    //    BindParameterFromValue(parameter, GetValueFromProperty(property, itemProperty, key, GetExpressionType(parameter.ParameterType)), factoryContext, source);


    private static IReadOnlyList<object> AsReadOnlyList(IList<object> metadata)
    {
        if (metadata is IReadOnlyList<object> readOnlyList)
        {
            return readOnlyList;
        }

        return new List<object>(metadata);
    }

    private static TopicDelegateFactoryContext CreateFactoryContext(
        TopicDelegateFactoryOptions? options,
        TopicDelegateMetadataResult? metadataResult = null,
        Delegate? handler = null)
    {
        var provider = options?.ServiceProvider;
        var topicBuilder = options?.TopicBuilder;

        var factoryContext = new TopicDelegateFactoryContext
        {
            Handler = handler,
            ServiceProvider = provider,
            ServiceProviderIsService = provider.GetService<IServiceProviderIsService>(),
            TopicBuilder = topicBuilder,
            MetadataAlreadyInferred = metadataResult is not null,
        };

        return factoryContext;
    }

    internal static class TopicDelegateFactoryConstants
    {
        public const string KeyAttribute = "Key (Attribute)";
        public const string ValueAttribute = "Value (Attribute)";
        public const string HeaderAttribute = "Header (Attribute)";
        public const string ServiceAttribute = "Service (Attribute)";

        public const string ServiceParameter = "Services (Inferred)";
    }
}

public class TopicDelegateMetadataResult
{
    public IReadOnlyList<object> TopicMetadata { get; set; }
    public Type TopicKeyType { get; internal set; }
    public Type TopicValueType { get; internal set; }
}

public class TopicDelegateFactoryOptions
{
    public IServiceProvider? ServiceProvider { get; init; }
    public ITopicConsumerBuilder TopicBuilder { get; init; }
}

public class TopicDelegateFactoryContext
{
    public Delegate? Handler { get; init; }
    public IServiceProviderIsService? ServiceProviderIsService { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
    public ITopicConsumerBuilder? TopicBuilder { get; init; }
    public bool MetadataAlreadyInferred { get; init; }
    public Expression[] ArgumentExpressions { get; set; }
    public Type[] ArgumentTypes { get; set; }
    public Expression[] BoxedArgs { get; set; }
    public List<ParameterInfo> Parameters { get; set; }
    public Dictionary<string, string> TrackedParameters { get; set; } = new Dictionary<string, string>();
    public Expression MethodCall { get; internal set; }
    public Type KeyType { get; internal set; }
    public Type ValueType { get; internal set; }
}
