namespace MinimalKafka.Attributes;

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class FromValueAttribute : Attribute, IFromValueMetadata
{
}

public interface IFromValueMetadata
{

}
