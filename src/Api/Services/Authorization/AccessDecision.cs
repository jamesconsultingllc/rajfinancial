using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.Authorization;

/// <summary>
/// Represents the result of a resource-level access check performed by
/// <see cref="IAuthorizationService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use the static factory methods <see cref="Grant"/> and <see cref="Deny"/> to create instances.
/// This type is immutable and uses value equality via <c>record</c> semantics.
/// </para>
/// <para>
/// <b>OWASP A01:2025 — Broken Access Control</b>: This type captures the three-tier
/// authorization decision so callers can audit why access was granted or denied.
/// </para>
/// </remarks>
public sealed record AccessDecision
{
    /// <summary>
    /// Whether the access check passed.
    /// </summary>
    public required bool IsGranted { get; init; }

    /// <summary>
    /// The reason the access decision was made.
    /// </summary>
    public required AccessDecisionReason Reason { get; init; }

    /// <summary>
    /// The effective access level granted. Null when <see cref="IsGranted"/> is <c>false</c>.
    /// </summary>
    public AccessType? GrantedAccessLevel { get; private init; }

    /// <summary>
    /// Creates a granted access decision.
    /// </summary>
    /// <param name="reason">Why access was granted (must not be <see cref="AccessDecisionReason.Denied"/>).</param>
    /// <param name="grantedLevel">The effective access level.</param>
    /// <returns>An <see cref="AccessDecision"/> with <see cref="IsGranted"/> set to <c>true</c>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="reason"/> is <see cref="AccessDecisionReason.Denied"/>.
    /// </exception>
    public static AccessDecision Grant(AccessDecisionReason reason, AccessType grantedLevel)
    {
        if (reason == AccessDecisionReason.Denied)
            throw new ArgumentException("Grant reason cannot be Denied.", nameof(reason));

        return new AccessDecision
        {
            IsGranted = true,
            Reason = reason,
            GrantedAccessLevel = grantedLevel
        };
    }

    /// <summary>
    /// Creates a denied access decision.
    /// </summary>
    /// <returns>An <see cref="AccessDecision"/> with <see cref="IsGranted"/> set to <c>false</c>.</returns>
    public static AccessDecision Deny()
    {
        return new AccessDecision
        {
            IsGranted = false,
            Reason = AccessDecisionReason.Denied,
            GrantedAccessLevel = null
        };
    }
}
