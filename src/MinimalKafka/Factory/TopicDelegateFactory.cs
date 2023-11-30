using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka.Factory;

public class TopicDelegateFactory
{

    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    //private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;

    private static readonly ParameterExpression KafkaContextExpr = ParameterBindingMethodCache.KafkaContextExpr;
    private static readonly MemberExpression RequestServicesExpr = Expression.Property(KafkaContextExpr, typeof(KafkaContext).GetProperty(nameof(KafkaContext.RequestServices))!);
    private static readonly MemberExpression KeyExpr = Expression.Property(KafkaContextExpr, typeof(KafkaContext).GetProperty(nameof(KafkaContext.Key))!);


    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");

    public static TopicDelegateMetadataResult InferMetaData(
        MethodInfo methodInfo,
        TopicDelegateFactoryOptions? options = null)
    {
        var factoryContext = CreateFactoryContext(options);

        factoryContext.ArgumentExpressions = CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        return new TopicDelegateMetadataResult
        {
            //TopicMetadata = AsReadOnlyList(factoryContext.TopicBuilder.Metadata),
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

        return CreateTopicDelegateResult(finalTopicDelegate);
    }

    private static TopicDelegateResult CreateTopicDelegateResult(TopicDelegate finalTopicDelegate)
    {
        return new TopicDelegateResult(finalTopicDelegate, null);
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

        if (parameterCustomAttributes.OfType<IFromKeyMetadata>().FirstOrDefault() is { } keyAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.KeyAttribute);

            return BindParameterFromKey(parameter, false, factoryContext);
        }
        else if (parameter.ParameterType == typeof(KafkaContext))
        {
            return KafkaContextExpr;
        }
        else
        {
            if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService)
            {
                if (serviceProviderIsService.IsService(parameter.ParameterType))
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.ServiceParameter);
                    return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
                }
            }

            return Expression.Empty();
        }
    }

    private static Expression BindParameterFromKey(ParameterInfo parameter, bool allowEmpty, TopicDelegateFactoryContext factoryContext)
    {
        return Expression.Convert(Expression.Property(KafkaContextExpr, "Key"), parameter.ParameterType);
    }

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

    internal class TopicDelegateFactoryConstants
    {
        public const string KeyAttribute = "Key (Attribute)";

        public const string ServiceParameter = "Services (Inferred)";
    }
}



public class TopicDelegateMetadataResult
{
    public IReadOnlyList<object> TopicMetadata { get; set; }
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
}
