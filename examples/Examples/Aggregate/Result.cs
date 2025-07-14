namespace Examples.Aggregate;

public static class Result
{
    public static Result<TResult> Failed<TResult>(TResult value, params string[] errorMessage)
        => new()
        {
            State = value,
            ErrorMessage = errorMessage,
            IsSuccess = false
        };
}

public class Result<T>
{
    public required T State { get; init;  }
    public bool IsSuccess { get; init; } = true;
    public string[] ErrorMessage { get; init; } = [];

    public static implicit operator Result<T>(T value)
        => new()
        {
            State = value,
            IsSuccess = true
        };
}
