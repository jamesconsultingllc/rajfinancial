namespace RajFinancial.Shared.Entities;

/// <summary>
///     Depreciation methods supported for asset valuation.
/// </summary>
public enum DepreciationMethod
{
    /// <summary>
    ///     No depreciation applied. Asset value is managed manually.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Straight-line depreciation: equal expense each period over the useful life.
    ///     Formula: (Cost - Salvage Value) / Useful Life
    /// </summary>
    StraightLine = 1,

    /// <summary>
    ///     Declining balance depreciation: accelerated method applying a fixed rate
    ///     to the remaining book value each period (typically double the straight-line rate).
    /// </summary>
    DecliningBalance = 2,

    /// <summary>
    ///     Modified Accelerated Cost Recovery System: IRS-defined depreciation schedules
    ///     for tax purposes, using specific recovery periods and conventions.
    /// </summary>
    Macrs = 3
}
