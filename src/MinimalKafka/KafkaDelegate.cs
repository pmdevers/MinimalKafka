namespace MinimalKafka;

public delegate Task KafkaDelegate(KafkaContext context);

public delegate Task KafkaMiddleware(KafkaContext context, Func<Task> next);