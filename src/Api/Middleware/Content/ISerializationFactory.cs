namespace RajFinancial.Api.Middleware.Content;

/// <summary>
/// Factory for serialization based on content type and environment.
/// </summary>
public interface ISerializationFactory
{
    /// <summary>
    /// Gets the preferred content type based on Accept header and environment.
    /// </summary>
    string GetPreferredContentType(string? acceptHeader);

    /// <summary>
    /// Serializes an object to bytes based on content type.
    /// </summary>
    byte[] Serialize<T>(T value, string contentType);

    /// <summary>
    /// Deserializes bytes to an object based on content type.
    /// </summary>
    T? Deserialize<T>(byte[] data, string contentType);
}