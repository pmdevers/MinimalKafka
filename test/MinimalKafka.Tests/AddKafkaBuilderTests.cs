using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream;

namespace MinimalKafka.Tests;
public class AddKafkaBuilderTests
{
    public class WithStreamStore
    {
        [Fact]
        public void Should_Throw_If_Type_IsNot_IStreamStore()
        {
            var serviceCollection = new ServiceCollection();

            var builder = new AddKafkaBuilder(serviceCollection, []);

            var action = () => builder.WithStreamStore(typeof(string));

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Should_Add_To_ServiceCollection_if_is_IStreamStore()
        {
            var serviceCollection = new ServiceCollection();

            var builder = new AddKafkaBuilder(serviceCollection, []);

            builder.WithStreamStore(typeof(TestStore));

            serviceCollection.Should().HaveCount(1);
        }
    }

    public class WithInMemoryStore
    {
        [Fact]
        public void Should_Call_WithStreamStore()
        {
            var builder = Substitute.For<IAddKafkaBuilder>();

            builder.WithInMemoryStore();


            builder.Received(1).WithStreamStore(typeof(InMemoryStore<,>));
        }
    }

    public class TestStore : IStreamStore<string, string>
    {
        public ValueTask<string> AddOrUpdate(string key, Func<string, string> create, Func<string, string, string> update)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<string> FindAsync(Func<string, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public ValueTask<string?> FindByIdAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
