using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Maps <see cref="Asset"/> entities to their DTO representations.
///     Detail mapping folds in computed depreciation values via
///     <see cref="DepreciationCalculator"/>.
/// </summary>
internal static class AssetMapper
{
    /// <summary>
    ///     Maps an <see cref="Asset"/> to its summary <see cref="AssetDto"/>.
    /// </summary>
    public static AssetDto ToDto(this Asset asset) => new()
    {
        Id = asset.Id,
        Name = asset.Name,
        Type = asset.Type,
        CurrentValue = asset.CurrentValue.FromMoney(),
        PurchasePrice = asset.PurchasePrice.FromMoney(),
        PurchaseDate = asset.PurchaseDate?.UtcDateTime,
        Description = asset.Description,
        Location = asset.Location,
        AccountNumber = asset.AccountNumber,
        InstitutionName = asset.InstitutionName,
        IsDepreciable = asset is DepreciableAsset,
        IsDisposed = asset.IsDisposed,
        // TODO: populate when beneficiary feature is implemented
        HasBeneficiaries = false,
        CreatedAt = asset.CreatedAt.UtcDateTime,
        UpdatedAt = asset.UpdatedAt?.UtcDateTime,
    };

    /// <summary>
    ///     Maps an <see cref="Asset"/> to its detailed <see cref="AssetDetailDto"/>,
    ///     including computed depreciation fields for <see cref="DepreciableAsset"/>.
    /// </summary>
    public static AssetDetailDto ToDetailDto(this Asset asset)
    {
        var depreciable = asset as DepreciableAsset;
        var depreciation = depreciable is not null
            ? DepreciationCalculator.Calculate(depreciable)
            : null;

        return new AssetDetailDto
        {
            Id = asset.Id,
            Name = asset.Name,
            Type = asset.Type,
            CurrentValue = asset.CurrentValue.FromMoney(),
            PurchasePrice = asset.PurchasePrice.FromMoney(),
            PurchaseDate = asset.PurchaseDate?.UtcDateTime,
            Description = asset.Description,
            Location = asset.Location,
            AccountNumber = asset.AccountNumber,
            InstitutionName = asset.InstitutionName,

            IsDepreciable = depreciable is not null,
            DepreciationMethod = depreciable?.DepreciationMethod,
            SalvageValue = depreciable?.SalvageValue.FromMoney(),
            UsefulLifeMonths = depreciable?.UsefulLifeMonths,
            InServiceDate = depreciable?.InServiceDate?.UtcDateTime,

            AccumulatedDepreciation = depreciation?.AccumulatedDepreciation.FromMoney(),
            BookValue = depreciation?.BookValue.FromMoney(),
            MonthlyDepreciation = depreciation?.MonthlyDepreciation.FromMoney(),
            DepreciationPercentComplete = (double?)depreciation?.DepreciationPercentComplete,

            IsDisposed = asset.IsDisposed,
            DisposalDate = asset.DisposalDate?.UtcDateTime,
            DisposalPrice = asset.DisposalPrice.FromMoney(),
            DisposalNotes = asset.DisposalNotes,

            MarketValue = asset.MarketValue.FromMoney(),
            LastValuationDate = asset.LastValuationDate?.UtcDateTime,

            // TODO: populate when beneficiary feature is implemented
            HasBeneficiaries = false,
            Beneficiaries = [],

            CreatedAt = asset.CreatedAt.UtcDateTime,
            UpdatedAt = asset.UpdatedAt?.UtcDateTime,
        };
    }
}
