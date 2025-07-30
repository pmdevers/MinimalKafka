using Qowaiv.Customization;
using Qowaiv.Validation.Abstractions;

namespace Qowaiv.DomainModel.Kafka.Domain;


[Id<GuidBehavior, Guid>]
public partial struct TestId { }

public record Created(TestId Id);
public record NameChanged(string Name, string Desription);

public class TestAggregate : Aggregate<TestAggregate, TestId>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; }


    public TestAggregate() : this(TestId.Next(), new TestAggregateValidator())
    {
    }

    private TestAggregate(TestId id) : this(id, new TestAggregateValidator())
    {
    }

    private TestAggregate(TestId aggregateId, IValidator<TestAggregate> validator) : base(aggregateId, validator)
    {
    }

    public static Result<TestAggregate> Create(TestId id)
        => new TestAggregate(id)
            .Apply(Events.Add(new Created(id)));

    public Result<TestAggregate> ChangeName(string name, string description)
    {
        return Apply(Events.Add(new NameChanged(name, description)));
    }

    internal void When(Created @event)
    {
        // Left empty on purpose,
    }

    internal void When(NameChanged @event)
    {
        Name = @event.Name;
        Description = @event.Desription;
    }
}


public class TestAggregateValidator : IValidator<TestAggregate>
{
    public Result<TestAggregate> Validate(TestAggregate model)
    {
        return model;
    }
}