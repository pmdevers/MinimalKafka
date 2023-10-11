namespace MinimalKafka;

public interface ITopicHandler
{
    Delegate GetHandler();
}

public class TopicHandler : ITopicHandler
{
    private readonly Delegate _handler;

    public TopicHandler(Delegate handler)
    {
        _handler = handler;
    }

    public Delegate GetHandler()
    {
        return _handler;
    }
}
