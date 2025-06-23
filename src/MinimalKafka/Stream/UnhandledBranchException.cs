namespace MinimalKafka.Stream;

/// <summary>
/// Represents an exception that is thrown when a code branch is reached that was not expected or handled.
/// </summary>
/// <remarks>This exception is typically used to indicate a logical error in the program where an unexpected
/// branch of code execution has been reached. It can be used as a safeguard in scenarios such as exhaustive switch
/// statements where all possible cases should be handled.</remarks>
public class UnhandledBranchException() : Exception()
{
}