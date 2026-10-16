using Microsoft.Extensions.Options;

namespace MinimalKafka.Middleware.ClaimCheck;

/// <summary>File-based store suitable for development or a shared volume. Use a blob store implementation in production.</summary>
public sealed class FileSystemClaimCheckStore : IClaimCheckStore
{
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemClaimCheckStore"/> class.
    /// </summary>
    /// <param name="options">Configured claim-check options used to resolve the file store path.</param>
    public FileSystemClaimCheckStore(IOptions<ClaimCheckOptions> options)
    {
        _directory = options.Value.FileStorePath
            ?? Path.Combine(Path.GetTempPath(), "kafka-claim-check");
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc/>
    public async Task<string> StoreAsync(byte[] payload, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var path = PathFor(id);
        var temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, payload, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, path);
        return id;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RetrieveAsync(string claimId, CancellationToken cancellationToken)
    {
        var path = PathFor(claimId);
        return File.Exists(path)
            ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
            : null;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string claimId, CancellationToken cancellationToken)
    {
        File.Delete(PathFor(claimId));
        return Task.CompletedTask;
    }

    // Only GUID identifiers are accepted, which prevents path traversal via a crafted header.
    private string PathFor(string claimId)
    {
        if (!Guid.TryParseExact(claimId, "N", out var guid))
        {
            throw new ArgumentException("Invalid claim identifier.", nameof(claimId));
        }
        return Path.Combine(_directory, guid.ToString("N") + ".claim");
    }
}
