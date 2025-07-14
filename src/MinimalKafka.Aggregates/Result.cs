namespace MinimalKafka.Aggregates;

/// <summary>
/// Static helper class for creating <see cref="Result{TResult}"/> objects representing operation outcomes.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a failed <see cref="Result{TResult}"/> with the specified state and error messages.
    /// </summary>
    /// <typeparam name="TResult">The type of the result state.</typeparam>
    /// <param name="value">The state value associated with the result.</param>
    /// <param name="errorMessage">One or more error messages describing the failure.</param>
    /// <returns>
    /// A <see cref="Result{TResult}"/> representing a failed operation, containing the specified state and error messages.
    /// </returns>
    public static Result<TResult> Failed<TResult>(TResult value, params string[] errorMessage)
        => new()
        {
            State = value,
            ErrorMessage = errorMessage,
            IsSuccess = false
        };
}

/// <summary>
/// Represents the outcome of an operation, including its result value, success status, and error messages (if any).
/// </summary>
/// <typeparam name="T">The type of the result state.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets or sets the state value associated with the result.
    /// </summary>
    public required T State { get; init;  }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets or sets the collection of error messages if the operation failed.
    /// Defaults to an empty array.
    /// </summary>
    public string[] ErrorMessage { get; init; } = [];

    /// <summary>
    /// Implicitly converts a state value of type <typeparamref name="T"/> to a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The state value to wrap as a successful result.</param>
    public static implicit operator Result<T>(T value)
        => new()
        {
            State = value,
            IsSuccess = true
        };
}
