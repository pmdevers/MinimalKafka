using Examples.EventSourced.Generic;

namespace Examples.EventSourced;


public class TestEvent : IEvent<Guid, EventTypes>
{
    public Guid Id {get; init; }
    public EventTypes Type { get; init; }
}

public class TestState : IState<Guid>
{
    public Guid Id { get; set; }
}

public enum CommandType
{
    Create,
    Update,
    Delete,
    None
}

public class TestCommand : ICommand<Guid, CommandType>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CommandType Type { get; init; }
}