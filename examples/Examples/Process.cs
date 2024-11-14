using MinimalKafka.Stream;
using MinimalKafka;

namespace Examples;

public record MyAggregate(Guid Id) : Aggregate<Guid>(Id), IAggregate<Guid, MyAggregate>
{
    public static MyAggregate Create(KafkaContext context, Guid key)
    {
        return new MyAggregate(key);
    }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
}

public record ChangeName(string Name) : ICommand<MyAggregate>
{
    public MyAggregate Execute(MyAggregate aggregate)
    {
        return aggregate with
        {
            Name = Name,
        };
    }
};
public record ChangeSurName(string Surname) : ICommand<MyAggregate>
{
    public MyAggregate Execute(MyAggregate aggregate)
    {
        return aggregate with
        {
            SurName = Surname,
        };
    }
}
