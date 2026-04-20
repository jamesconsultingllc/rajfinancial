using MemoryPack;

namespace RajFinancial.Shared.Contracts.Entities.Business;

/// <summary>
///     State registration record for a business entity (SOS filing, annual report).
/// </summary>
[MemoryPackable]
public sealed partial record StateRegistration
{
    /// <summary>State abbreviation (e.g., "DE", "CA").</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>State registration or entity number.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Secretary of State filing number.</summary>
    public string? SosFilingNumber { get; init; }

    /// <summary>Date registered in this state.</summary>
    public DateTimeOffset? RegisteredDate { get; init; }

    /// <summary>Next annual report due date.</summary>
    public DateTimeOffset? AnnualReportDueDate { get; init; }

    /// <summary>Whether the entity is in good standing in this state.</summary>
    public bool IsInGoodStanding { get; init; }
}
