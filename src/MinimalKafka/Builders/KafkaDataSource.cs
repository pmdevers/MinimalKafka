
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;

namespace MinimalKafka.Builders;

internal class KafkaDataSource(IServiceProvider serviceProvider) : IKafkaDataSource
{
    private readonly List<KafkaProcessEntry> _entries = [];

    public IKafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler)
    {
        var conventions = new AddAfterProcessBuildConventionCollection();
        var finallyConventions = new AddAfterProcessBuildConventionCollection();

        _entries.Add(new()
        {
            TopicName = topicName,
            Delegate = handler,
            Conventions = conventions,
            FinallyConventions = finallyConventions
        });

        return new KafkaConventionBuilder(conventions, finallyConventions);
    }

    public IEnumerable<IKafkaProcess> GetProcesses()
    {
        var processes = new HashSet<KafkaConsumerKey>();

        foreach (var entry in _entries)
        {
            var builder = new KafkaBuilder(serviceProvider);
            var topicFormatter = serviceProvider.GetRequiredService<KafkaTopicFormatter>();

            foreach (var convention in entry.Conventions)
            {
                convention(builder);
            }

            var result = KafkaDelegateFactory.Create(entry.Delegate, new()
            {
                KafkaBuilder = builder,
                ServiceProvider = serviceProvider,
            });

            foreach (var finallyConvention in entry.FinallyConventions)
            {
                finallyConvention(builder);
            }

            var key = new KafkaConsumerKey() { TopicName = topicFormatter(entry.TopicName), GroupId = builder.GetGroupId(), ClientId = builder.GetClientId() };

            if (processes.Contains(key))
            {
                throw new InvalidOperationException($"Duplicate consumer: '{key}' try specifying different consumer group. ");
            }

            processes.Add(key);

            yield return KafkaProcessBuilder.Create(serviceProvider)
                .WithKey(key)
                .WithMetadata(builder.MetaData)
                .WithMiddleware(builder.Middlewares)
                .WithDelegate(result.Delegate)
                .Build();
        }
    }


    private struct KafkaProcessEntry()
    {
        public string TopicName { get; set; } = string.Empty;
        public Delegate Delegate { get; set; } = () => Task.CompletedTask;
        public required AddAfterProcessBuildConventionCollection Conventions { get; init; }
        public required AddAfterProcessBuildConventionCollection FinallyConventions { get; init; }
    }

    private sealed class AddAfterProcessBuildConventionCollection :
        List<Action<IKafkaBuilder>>,
        ICollection<Action<IKafkaBuilder>>
    {
        public bool IsReadOnly { get; set; }

        void ICollection<Action<IKafkaBuilder>>.Add(Action<IKafkaBuilder> convention)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException($"{nameof(KafkaDataSource)} can not be modified after build.");
            }

            Add(convention);
        }
    }
}


internal static class MetadataExtensions
{
    public static string GetClientId(this IKafkaBuilder builder)
    {
        return builder.MetaData.OfType<ConfigMetadataAttribute>().First().BuildConsumerConfig().ClientId;
    }

    public static string GetGroupId(this IKafkaBuilder builder)
    {
        return builder.MetaData.OfType<ConfigMetadataAttribute>().First().BuildConsumerConfig().GroupId;
    }
}