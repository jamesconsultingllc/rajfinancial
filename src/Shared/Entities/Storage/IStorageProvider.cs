namespace RajFinancial.Shared.Entities.Storage;

/// <summary>
///     Abstraction for a cloud document storage backend (OneDrive, Google Drive, Dropbox, etc.).
///     Implementations handle provider-specific authentication and API calls.
/// </summary>
public interface IStorageProvider
{
    /// <summary>Identifies which concrete provider this implementation targets.</summary>
    StorageProvider ProviderType { get; }

    /// <summary>Opens a read stream for the document at <paramref name="path"/>.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Uploads <paramref name="content"/> to <paramref name="path"/>, overwriting if present.</summary>
    Task UploadAsync(string path, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the document at <paramref name="path"/>. No-op if it does not exist.</summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns <c>true</c> if a document exists at <paramref name="path"/>.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Enumerates document paths under <paramref name="prefix"/>.</summary>
    IAsyncEnumerable<string> ListAsync(string prefix, CancellationToken cancellationToken = default);
}
