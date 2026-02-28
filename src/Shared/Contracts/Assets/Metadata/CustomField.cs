using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A key-value pair for user-defined metadata on "Other" asset types.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CustomField
{
    /// <summary>Field name/key.</summary>
    [MemoryPackOrder(0)]
    public required string Key { get; init; }

    /// <summary>Field value.</summary>
    [MemoryPackOrder(1)]
    public required string Value { get; init; }
}
