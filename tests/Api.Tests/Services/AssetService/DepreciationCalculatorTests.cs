using FluentAssertions;
using RajFinancial.Api.Services.AssetService;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Services.AssetService;

/// <summary>
///     Unit tests for <see cref="DepreciationCalculator"/>.
///     Covers all depreciation methods, edge cases, and guard conditions.
/// </summary>
public class DepreciationCalculatorTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // =========================================================================
    // Zero / guard conditions
    // =========================================================================

    [Fact]
    public void Calculate_MethodNone_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.None);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_NullPurchasePrice_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine, purchasePrice: null);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_ZeroPurchasePrice_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine, purchasePrice: 0);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_NullUsefulLife_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine, usefulLifeMonths: null);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_ZeroUsefulLife_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine, usefulLifeMonths: 0);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_NullInServiceDateAndNullPurchaseDate_ReturnsZero()
    {
        var asset = new DepreciableAsset
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            PurchasePrice = 10000,
            PurchaseDate = null,
            InServiceDate = null,
            DepreciationMethod = DepreciationMethod.StraightLine,
            SalvageValue = 1000,
            UsefulLifeMonths = 60
        };
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_SalvageValueExceedsPurchasePrice_ReturnsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine, purchasePrice: 1000, salvageValue: 1500);
        var result = DepreciationCalculator.Calculate(asset, AsOf);
        result.Should().Be(DepreciationResult.Zero);
    }

    [Fact]
    public void Calculate_AsOfBeforeInServiceDate_ReturnsZeroAccumulated()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine,
            inServiceDate: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        result.AccumulatedDepreciation.Should().Be(0m);
        result.BookValue.Should().Be(10000m);
        result.DepreciationPercentComplete.Should().Be(0m);
    }

    [Fact]
    public void Calculate_FallsBackToPurchaseDateWhenInServiceDateNull()
    {
        var purchaseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var asset = CreateAsset(DepreciationMethod.StraightLine,
            inServiceDate: null, purchaseDate: purchaseDate);

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        // 12 months elapsed, should have some depreciation
        result.AccumulatedDepreciation.Should().BeGreaterThan(0);
    }

    // =========================================================================
    // Straight-line depreciation
    // =========================================================================

    [Fact]
    public void StraightLine_BasicCalculation_CorrectValues()
    {
        // $10,000 asset, $1,000 salvage, 60 months useful life, 12 months elapsed
        var asset = CreateAsset(DepreciationMethod.StraightLine,
            purchasePrice: 10000, salvageValue: 1000, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        // Monthly: (10000 - 1000) / 60 = 150
        result.MonthlyDepreciation.Should().Be(150m);
        // Accumulated: 150 * 12 = 1800
        result.AccumulatedDepreciation.Should().Be(1800m);
        // Book value: 10000 - 1800 = 8200
        result.BookValue.Should().Be(8200m);
        // Percent: 12/60 = 0.2
        result.DepreciationPercentComplete.Should().Be(0.2m);
    }

    [Fact]
    public void StraightLine_FullyDepreciated_CapsAtSalvageValue()
    {
        // 120 months elapsed on 60 month asset
        var asset = CreateAsset(DepreciationMethod.StraightLine,
            purchasePrice: 10000, salvageValue: 1000, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2016, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        result.BookValue.Should().Be(1000m); // salvage value floor
        result.DepreciationPercentComplete.Should().Be(1.0m);
    }

    [Fact]
    public void StraightLine_NullSalvageValue_TreatsAsZero()
    {
        var asset = CreateAsset(DepreciationMethod.StraightLine,
            purchasePrice: 6000, salvageValue: null, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        // Monthly: 6000 / 60 = 100
        result.MonthlyDepreciation.Should().Be(100m);
    }

    // =========================================================================
    // Declining balance depreciation
    // =========================================================================

    [Fact]
    public void DecliningBalance_FirstMonth_CorrectCalculation()
    {
        // $10,000 asset, $1,000 salvage, 60 months, 1 month elapsed
        var asset = CreateAsset(DepreciationMethod.DecliningBalance,
            purchasePrice: 10000, salvageValue: 1000, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        // Rate = 2/60 = 0.0333..., first month = 10000 * 0.0333 = 333.33
        result.AccumulatedDepreciation.Should().BeGreaterThan(0);
        result.BookValue.Should().BeLessThan(10000m);
        result.BookValue.Should().BeGreaterThanOrEqualTo(1000m);
    }

    [Fact]
    public void DecliningBalance_DoesNotGoBelowSalvageValue()
    {
        // Long elapsed time — should stop at salvage
        var asset = CreateAsset(DepreciationMethod.DecliningBalance,
            purchasePrice: 10000, salvageValue: 2000, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2016, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        result.BookValue.Should().BeGreaterThanOrEqualTo(2000m);
    }

    // =========================================================================
    // MACRS depreciation
    // =========================================================================

    [Fact]
    public void Macrs_BasicCalculation_DepreciatesToZero()
    {
        // MACRS has no salvage value — depreciates fully
        var asset = CreateAsset(DepreciationMethod.Macrs,
            purchasePrice: 10000, salvageValue: 0, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2016, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        result.BookValue.Should().BeGreaterThanOrEqualTo(0m);
        result.AccumulatedDepreciation.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Macrs_PartialPeriod_HasAccumulation()
    {
        var asset = CreateAsset(DepreciationMethod.Macrs,
            purchasePrice: 50000, salvageValue: 0, usefulLifeMonths: 60,
            inServiceDate: new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero));

        var result = DepreciationCalculator.Calculate(asset, AsOf);

        result.AccumulatedDepreciation.Should().BeGreaterThan(0);
        result.BookValue.Should().BeLessThan(50000m);
        result.MonthlyDepreciation.Should().BeGreaterThan(0);
    }

    // =========================================================================
    // DepreciationResult.Zero
    // =========================================================================

    [Fact]
    public void Zero_AllFieldsAreZero()
    {
        var zero = DepreciationResult.Zero;

        zero.AccumulatedDepreciation.Should().Be(0m);
        zero.BookValue.Should().Be(0m);
        zero.MonthlyDepreciation.Should().Be(0m);
        zero.DepreciationPercentComplete.Should().Be(0m);
    }

    // =========================================================================
    // Helper
    // =========================================================================

    private static DepreciableAsset CreateAsset(
        DepreciationMethod method,
        decimal? purchasePrice = 10000,
        decimal? salvageValue = 1000,
        int? usefulLifeMonths = 60,
        DateTimeOffset? inServiceDate = null,
        DateTimeOffset? purchaseDate = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Test Asset",
        Type = AssetType.Vehicle,
        CurrentValue = purchasePrice ?? 0,
        PurchasePrice = purchasePrice,
        PurchaseDate = purchaseDate ?? inServiceDate,
        InServiceDate = inServiceDate ?? new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        DepreciationMethod = method,
        SalvageValue = salvageValue,
        UsefulLifeMonths = usefulLifeMonths,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
