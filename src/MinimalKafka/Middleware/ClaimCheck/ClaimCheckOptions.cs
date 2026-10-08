namespace MinimalKafka.Middleware.ClaimCheck;

/// <summary>Configuration for claim check middleware behavior.</summary>
public sealed class ClaimCheckOptions
{
    /// <summary>Default header used to store the claim check identifier.</summary>
    public const string DefaultHeaderName = "x-claim-check";

    /// <summary>Payloads strictly larger than this many bytes are moved to the claim store.</summary>
    public int ThresholdBytes { get; set; } = 256 * 1024;

    /// <summary>Header used to carry the claim check identifier.</summary>
    public string HeaderName { get; set; } = DefaultHeaderName;

    /// <summary>Removes the stored payload once the handler pipeline has completed successfully.</summary>
    public bool DeleteAfterConsume { get; set; }

    /// <summary>Directory used by <see cref="FileSystemClaimCheckStore"/>.</summary>
    public string? FileStorePath { get; set; }
}
