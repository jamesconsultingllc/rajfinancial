namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Response for role assignment errors.
/// </summary>
public sealed record CompleteRoleErrorResponse
{
    /// <summary>
    ///     Machine-readable error code for client-side localization.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    ///     Human-readable error message.
    /// </summary>
    public required string Error { get; init; }
}