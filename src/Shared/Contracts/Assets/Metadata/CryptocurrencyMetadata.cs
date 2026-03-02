using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for cryptocurrency assets.
///     Requires at least one <see cref="CryptoHolding"/> in the <see cref="Holdings"/> array.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CryptocurrencyMetadata : IAssetMetadata
{
    /// <summary>Type of wallet or custody solution.</summary>
    [MemoryPackOrder(0)]
    public required CryptoWalletType WalletType { get; init; }

    /// <summary>Array of coin/token positions (at least one required).</summary>
    [MemoryPackOrder(1)]
    public required CryptoHolding[] Holdings { get; init; }
}
