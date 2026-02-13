namespace RajFinancial.Api.Middleware.Content;

/// <summary>
/// Factory for serialization based on content type and environment.
/// </summary>
public interface ISerializationFactory
{
    /// <summary>
    /// Gets the preferred content type based on Accept header and environment.
    /// </summary>
    /// <param name="acceptHeader">The Accept header value from the request.</param>
    /// <returns>The content type to use for the response.</returns>
    string GetPreferredContentType(string? acceptHeader);

    /// <summary>
    /// Serializes an object to bytes based on content type.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="contentType">The content type determining serialization format.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The serialized bytes.</returns>
    Task<byte[]> SerializeAsync<T>(T value, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes bytes to an object based on content type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="data">The bytes to deserialize.</param>
    /// <param name="contentType">The content type determining deserialization format.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The deserialized object, or default if data is empty.</returns>
    Task<T?> DeserializeAsync<T>(byte[] data, string contentType, CancellationToken cancellationToken = default);
}