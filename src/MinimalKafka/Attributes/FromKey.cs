namespace MinimalKafka.Attributes;

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class FromKeyAttribute : Attribute, IFromKeyMetadata
{
}

public interface IFromKeyMetadata
{

}
