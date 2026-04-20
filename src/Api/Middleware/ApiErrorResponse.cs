namespace RajFinancial.Api.Middleware;

/// <summary>
/// Standardized API error response.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Machine-readable error code for client-side localization.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable message (default English, clients localize by Code).
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional additional details (field errors, resource IDs, etc.).
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }

    /// <summary>
    /// Trace ID for debugging and support tickets.
    /// </summary>
    public string? TraceId { get; init; }
}