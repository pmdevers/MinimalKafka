namespace MinimalKafka.Builders;

public interface IKafkaDataSource
{
    IServiceProvider ServiceProvider { get; }
    KafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler);
    IEnumerable<IKafkaProcess> GetProceses();
}

public sealed class KafkaDataSource(IServiceProvider serviceProvider) : IKafkaDataSource
{
    private readonly List<KafkaProcessEntry> _entries = [];

    public IServiceProvider ServiceProvider => serviceProvider;

    public KafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler)
    {
        var conventions = new AddAfterProcessBuildConventionCollection();
        var finallyConventions = new AddAfterProcessBuildConventionCollection();

        _entries.Add(new()
        {
            TopicName = topicName,
            Delegate = handler,
            Conventions = conventions,
            FinallyConventions = finallyConventions,
        });

        return new KafkaConventionBuilder(conventions, finallyConventions);
    }

    public IEnumerable<IKafkaProcess> GetProceses()
    {
        foreach (var process in _entries)
        {
            var builder = new KafkaBuilder(serviceProvider);

            foreach (var convention in process.Conventions)
            {
                convention(builder);
            }

            var result = KafkaDelegateFactory.Create(process.Delegate, new()
            {
                ServiceProvider = serviceProvider,
                KafkaBuilder = builder
            });

            foreach (var convention in process.FinallyConventions)
            {
                convention(builder);
            }

            var consumer = KafkaConsumer.Create(new()
            {
                KeyType = result.KeyType,
                ValueType = result.ValueType,
                ServiceProvider = serviceProvider,
                TopicName = process.TopicName,
                Metadata = result.Metadata,
            });

            yield return KafkaProcess.Create(new()
            {
                Consumer = consumer,
                Delegate = result.Delegate,
            });
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
