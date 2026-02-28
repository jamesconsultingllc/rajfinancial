using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for intellectual property assets (patents, trademarks, copyrights).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record IntellectualPropertyMetadata : IAssetMetadata
{
    /// <summary>Type of intellectual property.</summary>
    [MemoryPackOrder(0)]
    public required IpType IpType { get; init; }

    /// <summary>Patent/trademark/copyright registration number.</summary>
    [MemoryPackOrder(1)]
    public string? RegistrationNumber { get; init; }

    /// <summary>Country or region (e.g. US, EU, WIPO).</summary>
    [MemoryPackOrder(2)]
    public string? Jurisdiction { get; init; }

    /// <summary>Date filed with the registering authority.</summary>
    [MemoryPackOrder(3)]
    public DateTime? FilingDate { get; init; }

    /// <summary>Date granted/registered.</summary>
    [MemoryPackOrder(4)]
    public DateTime? IssueDate { get; init; }

    /// <summary>Date when protection expires.</summary>
    [MemoryPackOrder(5)]
    public DateTime? ExpirationDate { get; init; }

    /// <summary>Current registration status.</summary>
    [MemoryPackOrder(6)]
    public IpStatus? Status { get; init; }

    /// <summary>Primary licensee name.</summary>
    [MemoryPackOrder(7)]
    public string? Licensee { get; init; }

    /// <summary>Royalty rate percentage.</summary>
    [MemoryPackOrder(8)]
    public double? RoyaltyRate { get; init; }

    /// <summary>Annual licensing/royalty income.</summary>
    [MemoryPackOrder(9)]
    public double? AnnualRevenue { get; init; }
}
