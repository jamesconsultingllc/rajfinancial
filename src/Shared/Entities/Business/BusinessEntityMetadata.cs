using MemoryPack;

namespace RajFinancial.Shared.Entities.Business;

/// <summary>
///     Business-specific metadata stored as a JSON column on the Entity table.
/// </summary>
[MemoryPackable]
public sealed partial record BusinessEntityMetadata
{
    /// <summary>Legal formation type (LLC, S-Corp, etc.).</summary>
    public BusinessFormationType EntityFormationType { get; init; }

    /// <summary>Employer Identification Number.</summary>
    public string? Ein { get; init; }

    /// <summary>Dun &amp; Bradstreet DUNS number.</summary>
    public string? DunsNumber { get; init; }

    /// <summary>North American Industry Classification System code.</summary>
    public string? NaicsCode { get; init; }

    /// <summary>Industry description.</summary>
    public string? Industry { get; init; }

    /// <summary>State where the entity was formed.</summary>
    public string? StateOfFormation { get; init; }

    /// <summary>Date of formation or incorporation.</summary>
    public DateTimeOffset? FormationDate { get; init; }

    /// <summary>Fiscal year end month (1–12). Null defaults to December.</summary>
    public int? FiscalYearEnd { get; init; }

    /// <summary>Registered agent name.</summary>
    public string? RegisteredAgentName { get; init; }

    /// <summary>Annual revenue (for classification purposes).</summary>
    public decimal? AnnualRevenue { get; init; }

    /// <summary>Number of employees.</summary>
    public int? NumberOfEmployees { get; init; }

    /// <summary>IRS tax classification.</summary>
    public TaxClassification? TaxClassification { get; init; }

    /// <summary>State registrations (SOS filings, annual reports).</summary>
    public List<StateRegistration>? Registrations { get; init; }
}
