using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A single tier in an employer retirement match structure.
///     Example: "100% match on first 6% of salary" = { MatchPercent = 100, OnFirst = 6.0 }.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record EmployerMatchTier
{
    /// <summary>Percentage the employer matches (e.g. 100 for 100%, 50 for 50%).</summary>
    [MemoryPackOrder(0)]
    public required double MatchPercent { get; init; }

    /// <summary>Of the first X% of salary (e.g. 6.0 means first 6%).</summary>
    [MemoryPackOrder(1)]
    public required double OnFirst { get; init; }
}
