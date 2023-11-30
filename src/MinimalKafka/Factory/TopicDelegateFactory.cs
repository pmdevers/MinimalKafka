using Confluent.Kafka;
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
    public static TopicDelegateMetadataResult InferMetaData(
        MethodInfo methodInfo,
        TopicDelegateFactoryOptions? options = null)
    {
        var factoryContext = CreateFactoryContext(options);

        factoryContext.ArgumentExpressions = CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        return new TopicDelegateMetadataResult
        {
            TopicMetadata = AsReadOnlyList(factoryContext.TopicBuilder.Metadata),
        };
    }

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

        if (parameterCustomAttributes.OfType<IFromKeyMetadata>() is { } keyAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, TopicDelegateFactoryConstants.KeyAttribute);

            return BindParameterFromKey(parameter, keyAttribute.AllowEmpty, factoryContext);
        }
    }

    private static Expression BindParameterFromKey(ParameterInfo parameter, bool allowEmpty, TopicDelegateFactoryContext factoryContext)
    {
        
    }

    private static object AsReadOnlyList(object metadata)
    {
        throw new NotImplementedException();
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
    }
}



public class TopicDelegateMetadataResult
{
    public TopicMetadata TopicMetadata { get; set; }
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
    public Dictionary<string, string> TrackedParameters { get; set; }
}
