namespace MinimalKafka.Middleware.ClaimCheck;

/// <summary>
/// Abstraction for storing and retrieving large message payloads using claim-check identifiers.
/// </summary>
public interface IClaimCheckStore
{
    /// <summary>
    /// Stores the specified payload and returns an opaque claim identifier.
    /// </summary>
    /// <param name="payload">The payload bytes to persist.</param>
    /// <param name="cancellationToken">A token used to cancel the store operation.</param>
    /// <returns>
    /// An opaque claim identifier that can later be passed to <see cref="RetrieveAsync"/>.
    /// </returns>
    Task<string> StoreAsync(byte[] payload, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a payload by claim identifier.
    /// </summary>
    /// <param name="claimId">The opaque claim identifier returned by <see cref="StoreAsync"/>.</param>
    /// <param name="cancellationToken">A token used to cancel the retrieve operation.</param>
    /// <returns>The payload, or <see langword="null"/> when the claim does not exist.</returns>
    Task<byte[]?> RetrieveAsync(string claimId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the stored payload associated with the specified claim identifier.
    /// </summary>
    /// <param name="claimId">The opaque claim identifier returned by <see cref="StoreAsync"/>.</param>
    /// <param name="cancellationToken">A token used to cancel the delete operation.</param>
    Task DeleteAsync(string claimId, CancellationToken cancellationToken);
}