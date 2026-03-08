using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for vehicle assets (cars, trucks, motorcycles, etc.).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record VehicleMetadata : IAssetMetadata
{
    /// <summary>Vehicle Identification Number (17 characters).</summary>
    [MemoryPackOrder(0)]
    public string? Vin { get; init; }

    /// <summary>Vehicle manufacturer (e.g. Tesla, Ford, BMW).</summary>
    [MemoryPackOrder(1)]
    public required string Make { get; init; }

    /// <summary>Vehicle model (e.g. Model 3, F-150, X5).</summary>
    [MemoryPackOrder(2)]
    public required string Model { get; init; }

    /// <summary>Model year (4-digit).</summary>
    [MemoryPackOrder(3)]
    public required int Year { get; init; }

    /// <summary>Current odometer reading.</summary>
    [MemoryPackOrder(4)]
    public int? Mileage { get; init; }

    /// <summary>Exterior color.</summary>
    [MemoryPackOrder(5)]
    public string? Color { get; init; }

    /// <summary>License plate number.</summary>
    [MemoryPackOrder(6)]
    public string? LicensePlate { get; init; }
}
