namespace Examples.EventSourced;

public enum CommandType
{
    Create,
    Update,
    Delete,
    None
}

public record Command<TKey>
{
    public TKey Id { get; init; }
    public CommandType Type { get; init; }

}
