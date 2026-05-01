using System.Collections.ObjectModel;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     Canonical ActivitySource / Meter names for each instrumentation domain.
/// </summary>
/// <remarks>
///     Domain classes (AuthFunctions, EntityFunctions, etc.) reference these constants
///     when creating their <see cref="System.Diagnostics.ActivitySource"/> and
///     <see cref="System.Diagnostics.Metrics.Meter"/> instances, ensuring the names stay
///     in lockstep with the source/meter registration in <see cref="ObservabilityRegistration"/>.
///     See AGENTS.md "Observability" section for domain ownership.
/// </remarks>
internal static class ObservabilityDomains
{
    internal const string Auth = "RajFinancial.Api.Auth";
    internal const string Assets = "RajFinancial.Api.Assets";
    internal const string Entities = "RajFinancial.Api.Entities";
    internal const string UserProfile = "RajFinancial.Api.UserProfile";
    internal const string Middleware = "RajFinancial.Api.Middleware";
    internal const string ClientManagement = "RajFinancial.Api.ClientManagement";
    internal const string Authorization = "RajFinancial.Api.Authorization";

    /// <summary>All registered domain source/meter names as an immutable read-only view.</summary>
    internal static readonly IReadOnlyList<string> All = new ReadOnlyCollection<string>(
    [
        Auth,
        Assets,
        Entities,
        UserProfile,
        Middleware,
        ClientManagement,
        Authorization,
    ]);
}
