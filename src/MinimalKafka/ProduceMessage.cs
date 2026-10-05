namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
/// <param name="Topic"></param>
/// <param name="Key"></param>
/// <param name="Value"></param>
/// <param name="Headers"></param>
public record ProduceMessage(string Topic, object? Key, object? Value, Dictionary<string, string> Headers);
