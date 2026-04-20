namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Round-tripping helpers between <see cref="double"/> (wire format) and
///     <see cref="decimal"/> (storage format) for monetary values. Rounds to 2
///     decimal places to avoid floating-point representation artifacts (e.g.
///     0.1 → 0.100000000000000005...).
/// </summary>
internal static class MoneyConversion
{
    private const int MONEY_DECIMAL_PLACES = 2;

    /// <summary>
    ///     Converts a <see cref="double"/> money value to <see cref="decimal"/>,
    ///     rounded to 2 decimal places.
    /// </summary>
    public static decimal ToMoney(this double value) =>
        Math.Round((decimal)value, MONEY_DECIMAL_PLACES, MidpointRounding.AwayFromZero);

    /// <inheritdoc cref="ToMoney(double)"/>
    public static decimal? ToMoney(this double? value) =>
        value?.ToMoney();

    /// <summary>
    ///     Converts a <see cref="decimal"/> money value to <see cref="double"/>,
    ///     rounded to 2 decimal places.
    /// </summary>
    public static double FromMoney(this decimal value) =>
        (double)Math.Round(value, MONEY_DECIMAL_PLACES, MidpointRounding.AwayFromZero);

    /// <inheritdoc cref="FromMoney(decimal)"/>
    public static double? FromMoney(this decimal? value) =>
        value?.FromMoney();
}
