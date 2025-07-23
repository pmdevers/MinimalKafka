namespace Examples.EventSourced;

public record State<TKey>
{
    public TKey Id { get; set; }
}
