// ============================================================================
// RAJ Financial - Depreciation Calculator
// ============================================================================
// Pure/static computation of depreciation values. All methods are stateless
// and require no DI. Used by AssetService to populate computed fields on
// AssetDetailDto at read time.
// ============================================================================

using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Computes depreciation values for depreciable assets.
/// </summary>
/// <remarks>
///     All methods are static and pure — no database access, no side effects.
///     Depreciation is computed at read time rather than stored, keeping the
///     entity as the single source of truth for raw financial data.
/// </remarks>
public static class DepreciationCalculator
{
    /// <summary>
    ///     Computes depreciation details for a depreciable asset.
    /// </summary>
    /// <param name="asset">The depreciable asset entity.</param>
    /// <param name="asOf">The date to compute depreciation as of. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>
    ///     A <see cref="DepreciationResult"/> with computed values, or a zero result
    ///     if the asset lacks required fields for depreciation.
    /// </returns>
    public static DepreciationResult Calculate(DepreciableAsset asset, DateTimeOffset? asOf = null)
    {
        var now = asOf ?? DateTimeOffset.UtcNow;

        if (asset.DepreciationMethod == DepreciationMethod.None)
            return DepreciationResult.Zero;

        var purchasePrice = asset.PurchasePrice;
        if (purchasePrice is null or <= 0)
            return DepreciationResult.Zero;

        var usefulLifeMonths = asset.UsefulLifeMonths;
        if (usefulLifeMonths is null or <= 0)
            return DepreciationResult.Zero;

        var salvageValue = asset.SalvageValue ?? 0m;
        var inServiceDate = asset.InServiceDate ?? asset.PurchaseDate;
        if (inServiceDate is null)
            return DepreciationResult.Zero;

        var depreciableBasis = purchasePrice.Value - salvageValue;
        if (depreciableBasis <= 0)
            return DepreciationResult.Zero;

        var monthsElapsed = GetMonthsElapsed(inServiceDate.Value, now);
        if (monthsElapsed <= 0)
            return new DepreciationResult
            {
                AccumulatedDepreciation = 0m,
                BookValue = purchasePrice.Value,
                MonthlyDepreciation = CalculateMonthlyRate(asset.DepreciationMethod, depreciableBasis, usefulLifeMonths.Value),
                DepreciationPercentComplete = 0m
            };

        return asset.DepreciationMethod switch
        {
            DepreciationMethod.StraightLine => CalculateStraightLine(
                purchasePrice.Value, depreciableBasis, salvageValue, usefulLifeMonths.Value, monthsElapsed),
            DepreciationMethod.DecliningBalance => CalculateDecliningBalance(
                purchasePrice.Value, salvageValue, usefulLifeMonths.Value, monthsElapsed),
            DepreciationMethod.Macrs => CalculateMacrs(
                purchasePrice.Value, usefulLifeMonths.Value, monthsElapsed),
            _ => DepreciationResult.Zero
        };
    }

    private static DepreciationResult CalculateStraightLine(
        decimal purchasePrice, decimal depreciableBasis, decimal salvageValue,
        int usefulLifeMonths, int monthsElapsed)
    {
        var monthlyDepreciation = depreciableBasis / usefulLifeMonths;
        var cappedMonths = Math.Min(monthsElapsed, usefulLifeMonths);
        var accumulated = monthlyDepreciation * cappedMonths;
        var bookValue = purchasePrice - accumulated;

        return new DepreciationResult
        {
            AccumulatedDepreciation = Math.Round(accumulated, 2),
            BookValue = Math.Max(Math.Round(bookValue, 2), salvageValue),
            MonthlyDepreciation = Math.Round(monthlyDepreciation, 2),
            DepreciationPercentComplete = Math.Min((decimal)cappedMonths / usefulLifeMonths, 1.0m)
        };
    }

    private static DepreciationResult CalculateDecliningBalance(
        decimal purchasePrice, decimal salvageValue,
        int usefulLifeMonths, int monthsElapsed)
    {
        // Double declining balance: rate = 2 / useful life in months
        var monthlyRate = 2.0m / usefulLifeMonths;
        var bookValue = purchasePrice;
        var cappedMonths = Math.Min(monthsElapsed, usefulLifeMonths);
        decimal currentMonthlyDepreciation = 0;

        for (var i = 0; i < cappedMonths; i++)
        {
            currentMonthlyDepreciation = bookValue * monthlyRate;

            // Don't depreciate below salvage value
            if (bookValue - currentMonthlyDepreciation < salvageValue)
            {
                currentMonthlyDepreciation = bookValue - salvageValue;
                bookValue = salvageValue;
                break;
            }

            bookValue -= currentMonthlyDepreciation;
        }

        var accumulated = purchasePrice - bookValue;

        return new DepreciationResult
        {
            AccumulatedDepreciation = Math.Round(accumulated, 2),
            BookValue = Math.Round(bookValue, 2),
            MonthlyDepreciation = Math.Round(currentMonthlyDepreciation, 2),
            DepreciationPercentComplete = Math.Min((decimal)cappedMonths / usefulLifeMonths, 1.0m)
        };
    }

    private static DepreciationResult CalculateMacrs(
        decimal purchasePrice, int usefulLifeMonths, int monthsElapsed)
    {
        // Simplified MACRS using half-year convention with 200% declining balance
        // switching to straight-line when it yields a larger deduction.
        // MACRS depreciates to zero (no salvage value).
        var usefulLifeYears = usefulLifeMonths / 12.0m;
        var annualRate = usefulLifeYears > 0 ? 2.0m / usefulLifeYears : 0;
        var bookValue = purchasePrice;
        var cappedMonths = Math.Min(monthsElapsed, usefulLifeMonths);
        decimal currentMonthlyDepreciation = 0;

        for (var i = 0; i < cappedMonths; i++)
        {
            // Half-year convention: first and last year get half depreciation
            var isFirstMonth = i == 0;
            var factor = isFirstMonth ? 0.5m : 1.0m;

            var decliningAmount = bookValue * (annualRate / 12.0m) * factor;
            var remainingMonths = usefulLifeMonths - i;
            var straightLineAmount = remainingMonths > 0 ? bookValue / remainingMonths * factor : 0;

            // Switch to straight-line when it yields more
            currentMonthlyDepreciation = Math.Max(decliningAmount, straightLineAmount);

            if (bookValue - currentMonthlyDepreciation < 0)
            {
                currentMonthlyDepreciation = bookValue;
                bookValue = 0;
                break;
            }

            bookValue -= currentMonthlyDepreciation;
        }

        var accumulated = purchasePrice - bookValue;

        return new DepreciationResult
        {
            AccumulatedDepreciation = Math.Round(accumulated, 2),
            BookValue = Math.Round(bookValue, 2),
            MonthlyDepreciation = Math.Round(currentMonthlyDepreciation, 2),
            DepreciationPercentComplete = Math.Min((decimal)cappedMonths / usefulLifeMonths, 1.0m)
        };
    }

    private static decimal CalculateMonthlyRate(
        DepreciationMethod method, decimal depreciableBasis, int usefulLifeMonths) =>
        method switch
        {
            DepreciationMethod.StraightLine => Math.Round(depreciableBasis / usefulLifeMonths, 2),
            _ => 0m // DecliningBalance and MACRS rates vary by period
        };

    private static int GetMonthsElapsed(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) return 0;
        return ((end.Year - start.Year) * 12) + end.Month - start.Month;
    }
}

/// <summary>
///     Result of a depreciation calculation for a single asset.
/// </summary>
public sealed record DepreciationResult
{
    /// <summary>Total depreciation accumulated from in-service date to the calculation date.</summary>
    public required decimal AccumulatedDepreciation { get; init; }

    /// <summary>Current book value: PurchasePrice - AccumulatedDepreciation.</summary>
    public required decimal BookValue { get; init; }

    /// <summary>Depreciation expense per month under the current method.</summary>
    public required decimal MonthlyDepreciation { get; init; }

    /// <summary>Percentage of useful life elapsed (0.0 to 1.0).</summary>
    public required decimal DepreciationPercentComplete { get; init; }

    /// <summary>A zero-value result for assets that cannot be depreciated.</summary>
    public static DepreciationResult Zero { get; } = new()
    {
        AccumulatedDepreciation = 0m,
        BookValue = 0m,
        MonthlyDepreciation = 0m,
        DepreciationPercentComplete = 0m
    };
}
