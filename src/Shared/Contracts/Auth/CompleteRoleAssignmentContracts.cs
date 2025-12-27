namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
/// Request to complete role assignment after user creation.
/// </summary>
public sealed record CompleteRoleRequest
{
    /// <summary>
    /// The Entra ID user identifier.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The role to assign (Client, Advisor, or Administrator).
    /// </summary>
    public required string Role { get; init; }
}

/// <summary>
/// Response from a successful role assignment.
/// </summary>
public sealed record CompleteRoleResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The role that was assigned.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Optional message providing additional context.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Response for role assignment errors.
/// </summary>
public sealed record CompleteRoleErrorResponse
{
    /// <summary>
    /// Machine-readable error code for client-side localization.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Error { get; init; }
}
