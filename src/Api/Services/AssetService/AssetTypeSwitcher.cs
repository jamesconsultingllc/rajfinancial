using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Rebuilds an <see cref="Asset"/> when its depreciable/non-depreciable
///     classification changes. A type switch requires a DELETE + INSERT because
///     the TPH discriminator column cannot be updated in-place.
/// </summary>
internal static class AssetTypeSwitcher
{
    /// <summary>
    ///     Builds a new <see cref="Asset"/> (or <see cref="DepreciableAsset"/>)
    ///     that preserves the identity, audit and disposal state of
    ///     <paramref name="existing"/> while applying the fields from
    ///     <paramref name="request"/>.
    /// </summary>
    /// <param name="existing">The current asset being replaced.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="nowIsDepreciable">
    ///     <c>true</c> when the replacement should be a <see cref="DepreciableAsset"/>.
    /// </param>
    public static Asset Recreate(this Asset existing, UpdateAssetRequest request, bool nowIsDepreciable)
    {
        Asset newAsset = nowIsDepreciable
            ? new DepreciableAsset
            {
                DepreciationMethod = request.DepreciationMethod!.Value,
                SalvageValue = request.SalvageValue.ToMoney(),
                UsefulLifeMonths = request.UsefulLifeMonths,
                InServiceDate = request.InServiceDate,
            }
            : new Asset();

        newAsset.Id = existing.Id;
        newAsset.UserId = existing.UserId;
        newAsset.CreatedAt = existing.CreatedAt;
        newAsset.UpdatedAt = DateTimeOffset.UtcNow;

        newAsset.IsDisposed = existing.IsDisposed;
        newAsset.DisposalDate = existing.DisposalDate;
        newAsset.DisposalPrice = existing.DisposalPrice;
        newAsset.DisposalNotes = existing.DisposalNotes;

        newAsset.Name = request.Name;
        newAsset.Type = request.Type;
        newAsset.CurrentValue = request.CurrentValue.ToMoney();
        newAsset.PurchasePrice = request.PurchasePrice.ToMoney();
        newAsset.PurchaseDate = request.PurchaseDate;
        newAsset.Description = request.Description;
        newAsset.Location = request.Location;
        newAsset.AccountNumber = request.AccountNumber;
        newAsset.InstitutionName = request.InstitutionName;
        newAsset.MarketValue = request.MarketValue.ToMoney();
        newAsset.LastValuationDate = request.LastValuationDate;

        return newAsset;
    }
}
