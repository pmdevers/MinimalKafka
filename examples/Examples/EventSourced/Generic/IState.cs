namespace Examples.EventSourced.Generic;

public interface IState<TKey>
{
    TKey Id { get; set; }
}
