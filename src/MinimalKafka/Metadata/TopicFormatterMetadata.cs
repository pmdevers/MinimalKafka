﻿namespace MinimalKafka.Metadata;

public interface ITopicFormatter
{
    string Format(string topicName);
}


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TopicFormatterMetadataAttribute(Func<string, string>? formatter = null) 
    : Attribute, ITopicFormatter
{
    public string Format(string topicName) => formatter?.Invoke(topicName) ?? topicName;

    public static TopicFormatterMetadataAttribute Default => new();
}