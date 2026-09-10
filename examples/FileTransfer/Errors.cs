public static class Errors
{
    public static InvalidOperationException MeasurementNotFound(Guid id) =>
        new($"Measurement with ID '{id}' was not found.");
}