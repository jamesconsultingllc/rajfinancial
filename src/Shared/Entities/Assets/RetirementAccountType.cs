using System.Text.Json.Serialization;

namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Retirement account plan types.
///     Uses Plan prefix for numeric-starting names (401k, 403b).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RetirementAccountType
{
    /// <summary>401(k) employer-sponsored plan.</summary>
    [JsonStringEnumMemberName("401k")]
    Plan401K,

    /// <summary>Traditional Individual Retirement Account.</summary>
    Ira,

    /// <summary>Roth Individual Retirement Account.</summary>
    RothIra,

    /// <summary>Simplified Employee Pension IRA.</summary>
    [JsonStringEnumMemberName("sep_ira")]
    SepIra,

    /// <summary>Defined-benefit pension plan.</summary>
    Pension,

    /// <summary>403(b) tax-sheltered annuity plan.</summary>
    [JsonStringEnumMemberName("403b")]
    Plan403B,

    /// <summary>Other retirement account type.</summary>
    Other
}
