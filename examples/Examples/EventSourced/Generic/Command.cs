namespace Examples.EventSourced.Generic;

public interface ICommand<TKey, TCommandType>
    where TKey : notnull
{
    TKey Id { get; }
    TCommandType Type { get; }

}
