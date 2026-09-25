using System.Text.Json;

public static class Errors
{
    public static InvalidOperationException MeasurementNotFound(Guid id) =>
        new($"Measurement with ID '{id}' was not found.");

    internal static Exception UnsupportedTokenTypeForType(JsonTokenType tokenType, Type typeToConvert)
    {
        throw new NotImplementedException();
    }

    internal static Exception ValueCannotBeEmpty(string v)
    {
        throw new NotImplementedException();
    }
}