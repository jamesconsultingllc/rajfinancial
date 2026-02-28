using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for the catch-all "Other" asset type.
///     Supports a user-defined category and key-value custom fields.
/// </summary>
/// <remarks>
///     Uses <c>KeyValuePair&lt;string, string&gt;[]</c> instead of <c>Dictionary</c>
///     for MemoryPack compatibility.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record OtherAssetMetadata : IAssetMetadata
{
    /// <summary>User-defined category label.</summary>
    [MemoryPackOrder(0)]
    public string? Category { get; init; }

    /// <summary>Key-value pairs for user-defined metadata fields.</summary>
    [MemoryPackOrder(1)]
    public CustomField[]? CustomFields { get; init; }
}
