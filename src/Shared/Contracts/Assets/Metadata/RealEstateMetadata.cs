using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for real estate assets (homes, condos, land, commercial).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record RealEstateMetadata : IAssetMetadata
{
    /// <summary>Street address line 1.</summary>
    [MemoryPackOrder(0)]
    public required string Address { get; init; }

    /// <summary>Apartment, suite, unit, etc.</summary>
    [MemoryPackOrder(1)]
    public string? Address2 { get; init; }

    /// <summary>City.</summary>
    [MemoryPackOrder(2)]
    public required string City { get; init; }

    /// <summary>State (2-letter code or full name).</summary>
    [MemoryPackOrder(3)]
    public required string State { get; init; }

    /// <summary>ZIP/postal code.</summary>
    [MemoryPackOrder(4)]
    public required string ZipCode { get; init; }

    /// <summary>ISO 3166 country code. Defaults to "US".</summary>
    [MemoryPackOrder(5)]
    public string Country { get; init; } = "US";

    /// <summary>Type of property.</summary>
    [MemoryPackOrder(6)]
    public required PropertyType PropertyType { get; init; }

    /// <summary>Living area in square feet.</summary>
    [MemoryPackOrder(7)]
    public int? SquareFeet { get; init; }

    /// <summary>Year the structure was built (4-digit).</summary>
    [MemoryPackOrder(8)]
    public int? YearBuilt { get; init; }

    /// <summary>Lot size description (e.g. "0.25 acres").</summary>
    [MemoryPackOrder(9)]
    public string? LotSize { get; init; }

    /// <summary>Number of bedrooms.</summary>
    [MemoryPackOrder(10)]
    public int? Bedrooms { get; init; }

    /// <summary>Number of bathrooms (supports half baths, e.g. 2.5).</summary>
    [MemoryPackOrder(11)]
    public double? Bathrooms { get; init; }
}
