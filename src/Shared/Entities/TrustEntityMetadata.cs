using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Trust-specific metadata stored as a JSON column on the Entity table.
/// </summary>
/// <remarks>
///     <para>
///         Trusts are modeled across seven orthogonal axes because real-world trusts stack
///         multiple independent attributes (e.g., a "Non-Grantor, Complex, Discretionary,
///         Irrevocable, Spendthrift Family Trust"):
///     </para>
///     <list type="bullet">
///         <item><see cref="Category" /> — Revocable vs Irrevocable.</item>
///         <item><see cref="CreationMethod" /> — Inter Vivos vs Testamentary.</item>
///         <item><see cref="Type" /> — Primary categorical (Family, AssetProtection, SpecialNeeds, etc.).</item>
///         <item><see cref="TaxStatus" /> — Grantor vs NonGrantor.</item>
///         <item><see cref="IncomeTreatment" /> — Simple vs Complex (non-grantor only).</item>
///         <item><see cref="DistributionRule" /> — Mandatory vs Discretionary.</item>
///         <item><see cref="Features" /> — Stackable provisions (Spendthrift, Crummey, BypassTrust, LifeEstate, ...).</item>
///     </list>
///     <para>
///         <see cref="Goal" /> captures the owner's motivation (avoid probate, protect assets, reduce taxes, ...)
///         and is informational only — it does not constrain the structural axes above.
///     </para>
/// </remarks>
[MemoryPackable]
public sealed partial record TrustEntityMetadata
{
    /// <summary>Revocable or Irrevocable.</summary>
    public TrustCategory Category { get; init; }

    /// <summary>How the trust was created — during life (Inter Vivos) or via a will (Testamentary).</summary>
    /// <remarks>
    ///     Testamentary trusts are always irrevocable once active (the grantor is deceased).
    ///     Validation enforces: <see cref="CreationMethod" /> == Testamentary ⇒ <see cref="Category" /> == Irrevocable.
    /// </remarks>
    public TrustCreationMethod CreationMethod { get; init; }

    /// <summary>Primary categorical type of the trust (Family, AssetProtection, Charitable, ...).</summary>
    public TrustType Type { get; init; }

    /// <summary>
    ///     Free-form description when <see cref="Type" /> is <see cref="TrustType.Other" />
    ///     (e.g., for uncommon or hybrid trust structures).
    /// </summary>
    public string? OtherTypeDescription { get; init; }

    /// <summary>
    ///     Grantor vs NonGrantor for income tax purposes.
    ///     Grantor trusts pass income through to the grantor; non-grantor trusts file their own 1041.
    /// </summary>
    public TrustTaxStatus TaxStatus { get; init; }

    /// <summary>
    ///     Simple (all income distributed annually) vs Complex (income may be accumulated).
    ///     Null for grantor trusts, where income flows to the grantor regardless.
    /// </summary>
    public TrustIncomeTreatment? IncomeTreatment { get; init; }

    /// <summary>Whether distributions to beneficiaries are mandatory or discretionary.</summary>
    public TrustDistributionRule DistributionRule { get; init; }

    /// <summary>Stackable trust features / provisions (Spendthrift, Crummey, BypassTrust, LifeEstate, ...).</summary>
    public TrustFeatures Features { get; init; } = TrustFeatures.None;

    /// <summary>Owner's motivating goals for establishing the trust (informational).</summary>
    public TrustGoal Goal { get; init; } = TrustGoal.None;

    /// <summary>Trust's EIN (if applicable — non-grantor trusts generally require one).</summary>
    public string? Ein { get; init; }

    /// <summary>Date the trust was established.</summary>
    public DateTimeOffset? TrustDate { get; init; }

    /// <summary>Jurisdiction (state) governing the trust.</summary>
    public string? Jurisdiction { get; init; }

    /// <summary>Initial or total funding amount.</summary>
    public decimal? FundingAmount { get; init; }

    /// <summary>Successor trustee succession plan description.</summary>
    public string? SuccessorTrusteePlan { get; init; }

    /// <summary>
    ///     Validates invariants that cannot be expressed at the type level.
    ///     Returns an error message, or <c>null</c> if valid.
    /// </summary>
    /// <remarks>
    ///     Rules enforced:
    ///     <list type="bullet">
    ///         <item>Testamentary trusts must be Irrevocable.</item>
    ///         <item><see cref="Type" /> == <see cref="TrustType.Other" /> requires <see cref="OtherTypeDescription" />.</item>
    ///         <item>Grantor trusts should not specify <see cref="IncomeTreatment" /> (Simple/Complex applies to non-grantor only).</item>
    ///     </list>
    /// </remarks>
    public string? Validate()
    {
        if (CreationMethod == TrustCreationMethod.Testamentary && Category != TrustCategory.Irrevocable)
        {
            return "Testamentary trusts must be irrevocable.";
        }

        if (Type == TrustType.Other && string.IsNullOrWhiteSpace(OtherTypeDescription))
        {
            return "OtherTypeDescription is required when Type is Other.";
        }

        if (TaxStatus == TrustTaxStatus.Grantor && IncomeTreatment.HasValue)
        {
            return "IncomeTreatment (Simple/Complex) does not apply to grantor trusts.";
        }

        return null;
    }
}
