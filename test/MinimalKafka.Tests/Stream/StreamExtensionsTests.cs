using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Tests.Stream;
public class StreamExtensionsTests
{
    public class MapStream
    {
        [Fact]
        public void On_KafkaBuilder_Should_return_StreamBuilder()
        {
            var kafkaBuilder  = new KafkaBuilder(EmptyServiceProvider.Instance);
            var result = StreamExtensions.MapStream<Guid, string>(kafkaBuilder, "topic-name");
            result.Should().BeOfType<StreamBuilder<Guid, string>>();
        }

        [Fact]
        public void On_ApplicationBuilder_should_return_StreamBuilder()
        {
            var serviceCollection = new ServiceCollection();
            var kafkaBuilder = new KafkaBuilder(EmptyServiceProvider.Instance);
            serviceCollection.AddSingleton<IKafkaBuilder>(kafkaBuilder);
                       
            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceCollection.BuildServiceProvider());

            var resul = app.ApplicationServices.GetRequiredService<IKafkaBuilder>();

                     
            var result = StreamExtensions.MapStream<Guid, string>(app, "topic-name");
            result.Should().BeOfType<StreamBuilder<Guid, string>>();
        }

    }
}


public class Pipeline<TContext>
{
    private readonly List<Func<TContext, Func<Task>, Task>> _components = [];
    private Func<TContext, Task>? _terminal;

    public Pipeline<TContext> Add(Func<TContext, Func<Task>, Task> func)
    {
        _components.Add(func);
        return this;
    }

    public Pipeline<TContext> Run(Func<TContext, Task> terminal)
    {
        _terminal = terminal;
        return this;
    }

    internal Task ExecuteAsync(TContext context)
    {
        if(_terminal == null)
            throw new InvalidOperationException("Pipeline must have an end via Run().");

        Func<Task> next =() => _terminal(context);

        for(int i = _components.Count - 1; i >= 0; i--)
        {
            var current = _components[i];
            var prevNext = next;
            next = () => current(context, prevNext);
        }

        return next();
    }
}