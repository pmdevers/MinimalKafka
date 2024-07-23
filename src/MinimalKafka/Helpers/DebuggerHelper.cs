namespace MinimalKafka.Helpers;
internal static class DebuggerHelpers
{
    public static string GetDebugText(string key, object value)
        => $"'{key}' - {value}";

}
