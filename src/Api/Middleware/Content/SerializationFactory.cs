using System.Text.Json;
using MemoryPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware.Content;

/// <summary>
/// Implementation of serialization factory supporting JSON and MemoryPack.
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy:</b>
/// <list type="bullet">
///   <item>Development: Always JSON for easy debugging in browser/Postman</item>
///   <item>Production: MemoryPack by default for performance, JSON if explicitly requested</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Benefits of MemoryPack:</b>
/// <list type="bullet">
///   <item>7-8x faster serialization than System.Text.Json</item>
///   <item>60% smaller payload sizes</item>
///   <item>Near-zero memory allocation</item>
/// </list>
/// </para>
/// </remarks>
public class SerializationFactory : ISerializationFactory
{
    public const string JsonContentType = "application/json";
    public const string MemoryPackContentType = "application/x-memorypack";

    private readonly IConfiguration configuration;
    private readonly ILogger<SerializationFactory> logger;
    private readonly JsonSerializerOptions jsonOptions;

    public SerializationFactory(
        IConfiguration configuration,
        ILogger<SerializationFactory> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Whether to use MemoryPack in production (configurable).
    /// </summary>
    private bool UseMemoryPackInProduction =>
        configuration.GetValue("Serialization:UseMemoryPackInProduction", true);

    /// <summary>
    /// Current environment name.
    /// </summary>
    private string Environment =>
        configuration.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development";

    /// <inheritdoc />
    public string GetPreferredContentType(string? acceptHeader)
    {
        // In development, always prefer JSON for debugging
        if (Environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Development environment: using JSON serialization");
            return JsonContentType;
        }

        // In production, check configuration and Accept header
        if (!UseMemoryPackInProduction)
        {
            return JsonContentType;
        }

        // If client explicitly requests only JSON, honor it
        if (!string.IsNullOrEmpty(acceptHeader))
        {
            var acceptsJson = acceptHeader.Contains(JsonContentType, StringComparison.OrdinalIgnoreCase);
            var acceptsMemoryPack = acceptHeader.Contains(MemoryPackContentType, StringComparison.OrdinalIgnoreCase);

            // Client only wants JSON
            if (acceptsJson && !acceptsMemoryPack)
            {
                return JsonContentType;
            }

            // Client explicitly wants MemoryPack
            if (acceptsMemoryPack)
            {
                return MemoryPackContentType;
            }
        }

        // Default to MemoryPack in production
        return MemoryPackContentType;
    }

    /// <inheritdoc />
    public byte[] Serialize<T>(T value, string contentType)
    {
        if (contentType.Equals(MemoryPackContentType, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Serializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Serialize(value);
        }

        logger.LogDebug("Serializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.SerializeToUtf8Bytes(value, jsonOptions);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data, string contentType)
    {
        if (data.Length == 0)
        {
            return default;
        }

        if (contentType.Equals(MemoryPackContentType, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Deserializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Deserialize<T>(data);
        }

        logger.LogDebug("Deserializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.Deserialize<T>(data, jsonOptions);
    }
}