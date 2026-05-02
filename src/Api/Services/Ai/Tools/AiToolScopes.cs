namespace RajFinancial.Api.Services.Ai.Tools;

/// <summary>
/// Canonical scope identifiers for tools registered with <see cref="IAiToolRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// A scope groups tools that belong to the same domain (e.g., all asset-management tools
/// under <see cref="Assets"/>). Future iterations of <c>IAiToolCallingClientFactory</c>
/// will accept a scope so a domain endpoint only exposes its own tools to the model —
/// preventing, for example, an Assets endpoint from leaking Accounts tools.
/// </para>
/// <para>
/// PR-2 wires every registered tool through <c>GetAll()</c>; per-scope filtering is the
/// next step in the B1 train. Always register through these constants — a typo'd literal
/// is silently allowed today but breaks future scope filtering.
/// </para>
/// </remarks>
public static class AiToolScopes
{
    /// <summary>Tools used by tests + diagnostic surfaces only. Never registered in production hosts.</summary>
    public const string Diagnostics = "diagnostics";

    /// <summary>Asset-management tools (D1).</summary>
    public const string Assets = "assets";

    /// <summary>Account-management tools (D2).</summary>
    public const string Accounts = "accounts";
}
