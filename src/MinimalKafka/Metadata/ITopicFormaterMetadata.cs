namespace MinimalKafka.Metadata;

/// <summary>
/// 
/// </summary>
internal interface ITopicFormaterMetadata
{    
    /// <summary>
    /// 
    /// </summary>
    Func<string, string> TopicFormatter { get; }
}
