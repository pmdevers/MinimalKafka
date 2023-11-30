namespace MinimalKafka.Attributes;

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class FromValueAttribute : Attribute
{
    public static bool HasAttribute(ParameterInfo parameter) =>
        parameter.Name.Equals("Value", StringComparison.CurrentCultureIgnoreCase) ||
        parameter.GetCustomAttribute<FromValueAttribute>() is not null;
}
