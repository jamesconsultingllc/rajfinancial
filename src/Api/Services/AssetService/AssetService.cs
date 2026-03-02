// ============================================================================
// RAJ Financial - Asset Service Implementation
// ============================================================================
// EF Core implementation of IAssetService with three-tier authorization.
// Handles CRUD operations for Asset and DepreciableAsset entities,
// mapping to/from DTOs and computing depreciation at read time.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     EF Core implementation of <see cref="IAssetService"/>.
/// </summary>
public class AssetService(
    ApplicationDbContext db,
    IAuthorizationService authorizationService,
    ILogger<AssetService> logger) : IAssetService
{
    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        AssetType? filterType = null,
        bool includeDisposed = false)
    {
        await AuthorizeReadAsync(requestingUserId, ownerUserId);

        var query = db.Assets
            .AsNoTracking()
            .Where(a => a.UserId == ownerUserId);

        if (filterType.HasValue)
            query = query.Where(a => a.Type == filterType.Value);

        if (!includeDisposed)
            query = query.Where(a => !a.IsDisposed);

        var assets = await query
            .OrderBy(a => a.Name)
            .ToListAsync();

        return assets.Select(MapToDto).ToList();
    }

    public async Task<AssetDetailDto?> GetAssetByIdAsync(Guid requestingUserId, Guid assetId)
    {
        var asset = await db.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset is null)
            return null;

        await AuthorizeReadAsync(requestingUserId, asset.UserId);

        return MapToDetailDto(asset);
    }

    public async Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request)
    {
        var isDepreciable = request.DepreciationMethod.HasValue
                            && request.DepreciationMethod != DepreciationMethod.None;

        Asset asset = isDepreciable
            ? new DepreciableAsset
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                Type = request.Type,
                CurrentValue = ToMoney(request.CurrentValue),
                PurchasePrice = ToMoney(request.PurchasePrice),
                PurchaseDate = request.PurchaseDate,
                Description = request.Description,
                Location = request.Location,
                AccountNumber = request.AccountNumber,
                InstitutionName = request.InstitutionName,
                MarketValue = ToMoney(request.MarketValue),
                LastValuationDate = request.LastValuationDate,
                CreatedAt = DateTimeOffset.UtcNow,
                DepreciationMethod = request.DepreciationMethod!.Value,
                SalvageValue = ToMoney(request.SalvageValue),
                UsefulLifeMonths = request.UsefulLifeMonths,
                InServiceDate = request.InServiceDate
            }
            : new Asset
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                Type = request.Type,
                CurrentValue = ToMoney(request.CurrentValue),
                PurchasePrice = ToMoney(request.PurchasePrice),
                PurchaseDate = request.PurchaseDate,
                Description = request.Description,
                Location = request.Location,
                AccountNumber = request.AccountNumber,
                InstitutionName = request.InstitutionName,
                MarketValue = ToMoney(request.MarketValue),
                LastValuationDate = request.LastValuationDate,
                CreatedAt = DateTimeOffset.UtcNow
            };

        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        logger.LogInformation("Asset {AssetId} created for user {UserId}", asset.Id, userId);

        return MapToDto(asset);
    }

    public async Task<AssetDto> UpdateAssetAsync(Guid requestingUserId, Guid assetId, UpdateAssetRequest request)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                    ?? throw NotFoundException.Asset(assetId);

        await AuthorizeWriteAsync(requestingUserId, asset.UserId);

        var nowIsDepreciable = request.DepreciationMethod.HasValue
                               && request.DepreciationMethod != DepreciationMethod.None;
        var wasDepreciable = asset is DepreciableAsset;

        // If the asset type is changing between depreciable and non-depreciable,
        // we need to delete and recreate due to TPH discriminator constraints.
        // Wrapped in a transaction to ensure atomicity of remove+add.
        // DbUpdateException is caught to handle concurrent type-switch race conditions
        // (e.g., two requests both attempting to switch the same asset's type simultaneously).
        if (nowIsDepreciable != wasDepreciable)
        {
            var newAsset = RecreateAssetWithNewType(asset, request, nowIsDepreciable);

            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.Assets.Remove(asset);
                await db.SaveChangesAsync();

                db.Assets.Add(newAsset);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex,
                    "Concurrent type-switch conflict for asset {AssetId}. Client should retry.",
                    assetId);

                throw new BusinessRuleException(
                    "ASSET_CONCURRENT_MODIFICATION",
                    $"Asset '{assetId}' was modified by another request. Please retry.");
            }

            logger.LogInformation("Asset {AssetId} type changed (depreciable={IsDepreciable}) for user {UserId}",
                assetId, nowIsDepreciable, asset.UserId);

            return MapToDto(newAsset);
        }

        // Update common fields
        asset.Name = request.Name;
        asset.Type = request.Type;
        asset.CurrentValue = ToMoney(request.CurrentValue);
        asset.PurchasePrice = ToMoney(request.PurchasePrice);
        asset.PurchaseDate = request.PurchaseDate;
        asset.Description = request.Description;
        asset.Location = request.Location;
        asset.AccountNumber = request.AccountNumber;
        asset.InstitutionName = request.InstitutionName;
        asset.MarketValue = ToMoney(request.MarketValue);
        asset.LastValuationDate = request.LastValuationDate;
        asset.UpdatedAt = DateTimeOffset.UtcNow;

        // Update depreciation fields if applicable
        if (asset is DepreciableAsset depreciable && nowIsDepreciable)
        {
            depreciable.DepreciationMethod = request.DepreciationMethod!.Value;
            depreciable.SalvageValue = ToMoney(request.SalvageValue);
            depreciable.UsefulLifeMonths = request.UsefulLifeMonths;
            depreciable.InServiceDate = request.InServiceDate;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Asset {AssetId} updated by user {UserId}", assetId, requestingUserId);

        return MapToDto(asset);
    }

    public async Task DeleteAssetAsync(Guid requestingUserId, Guid assetId)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                    ?? throw NotFoundException.Asset(assetId);

        await AuthorizeWriteAsync(requestingUserId, asset.UserId);

        db.Assets.Remove(asset);
        await db.SaveChangesAsync();

        logger.LogInformation("Asset {AssetId} deleted by user {UserId}", assetId, requestingUserId);
    }

    // =========================================================================
    // Authorization helpers
    // =========================================================================

    private async Task AuthorizeReadAsync(Guid requestingUserId, Guid ownerUserId)
    {
        var decision = await authorizationService.CheckAccessAsync(
            requestingUserId, ownerUserId, DataCategories.Assets, AccessType.Read);

        if (!decision.IsGranted)
            throw new ForbiddenException();
    }

    private async Task AuthorizeWriteAsync(Guid requestingUserId, Guid ownerUserId)
    {
        var decision = await authorizationService.CheckAccessAsync(
            requestingUserId, ownerUserId, DataCategories.Assets, AccessType.Full);

        if (!decision.IsGranted)
            throw new ForbiddenException();
    }

    // =========================================================================
    // Mapping helpers
    // =========================================================================

    private static AssetDto MapToDto(Asset asset) => new()
    {
        Id = asset.Id,
        Name = asset.Name,
        Type = asset.Type,
        CurrentValue = FromMoney(asset.CurrentValue),
        PurchasePrice = FromMoney(asset.PurchasePrice),
        PurchaseDate = asset.PurchaseDate?.UtcDateTime,
        Description = asset.Description,
        Location = asset.Location,
        AccountNumber = asset.AccountNumber,
        InstitutionName = asset.InstitutionName,
        IsDepreciable = asset is DepreciableAsset,
        IsDisposed = asset.IsDisposed,
        HasBeneficiaries = false, // TODO: populate when beneficiary feature is implemented
        CreatedAt = asset.CreatedAt.UtcDateTime,
        UpdatedAt = asset.UpdatedAt?.UtcDateTime
    };

    private static AssetDetailDto MapToDetailDto(Asset asset)
    {
        var isDepreciable = asset is DepreciableAsset;
        var depreciable = asset as DepreciableAsset;

        DepreciationResult? depreciation = null;
        if (depreciable is not null)
            depreciation = DepreciationCalculator.Calculate(depreciable);

        return new AssetDetailDto
        {
            Id = asset.Id,
            Name = asset.Name,
            Type = asset.Type,
            CurrentValue = FromMoney(asset.CurrentValue),
            PurchasePrice = FromMoney(asset.PurchasePrice),
            PurchaseDate = asset.PurchaseDate?.UtcDateTime,
            Description = asset.Description,
            Location = asset.Location,
            AccountNumber = asset.AccountNumber,
            InstitutionName = asset.InstitutionName,

            // Depreciation input fields
            IsDepreciable = isDepreciable,
            DepreciationMethod = depreciable?.DepreciationMethod,
            SalvageValue = FromMoney(depreciable?.SalvageValue),
            UsefulLifeMonths = depreciable?.UsefulLifeMonths,
            InServiceDate = depreciable?.InServiceDate?.UtcDateTime,

            // Depreciation computed fields
            AccumulatedDepreciation = FromMoney(depreciation?.AccumulatedDepreciation),
            BookValue = FromMoney(depreciation?.BookValue),
            MonthlyDepreciation = FromMoney(depreciation?.MonthlyDepreciation),
            DepreciationPercentComplete = (double?)depreciation?.DepreciationPercentComplete,

            // Disposal
            IsDisposed = asset.IsDisposed,
            DisposalDate = asset.DisposalDate?.UtcDateTime,
            DisposalPrice = FromMoney(asset.DisposalPrice),
            DisposalNotes = asset.DisposalNotes,

            // Valuation
            MarketValue = FromMoney(asset.MarketValue),
            LastValuationDate = asset.LastValuationDate?.UtcDateTime,

            // Beneficiaries — TODO: populate when beneficiary feature is implemented
            HasBeneficiaries = false,
            Beneficiaries = [],

            // Audit
            CreatedAt = asset.CreatedAt.UtcDateTime,
            UpdatedAt = asset.UpdatedAt?.UtcDateTime
        };
    }

    private static Asset RecreateAssetWithNewType(Asset existing, UpdateAssetRequest request, bool nowIsDepreciable)
    {
        Asset newAsset = nowIsDepreciable
            ? new DepreciableAsset
            {
                DepreciationMethod = request.DepreciationMethod!.Value,
                SalvageValue = ToMoney(request.SalvageValue),
                UsefulLifeMonths = request.UsefulLifeMonths,
                InServiceDate = request.InServiceDate
            }
            : new Asset();

        // Preserve identity and audit fields
        newAsset.Id = existing.Id;
        newAsset.UserId = existing.UserId;
        newAsset.CreatedAt = existing.CreatedAt;
        newAsset.UpdatedAt = DateTimeOffset.UtcNow;

        // Preserve disposal state
        newAsset.IsDisposed = existing.IsDisposed;
        newAsset.DisposalDate = existing.DisposalDate;
        newAsset.DisposalPrice = existing.DisposalPrice;
        newAsset.DisposalNotes = existing.DisposalNotes;

        // Apply updated fields from request
        newAsset.Name = request.Name;
        newAsset.Type = request.Type;
        newAsset.CurrentValue = ToMoney(request.CurrentValue);
        newAsset.PurchasePrice = ToMoney(request.PurchasePrice);
        newAsset.PurchaseDate = request.PurchaseDate;
        newAsset.Description = request.Description;
        newAsset.Location = request.Location;
        newAsset.AccountNumber = request.AccountNumber;
        newAsset.InstitutionName = request.InstitutionName;
        newAsset.MarketValue = ToMoney(request.MarketValue);
        newAsset.LastValuationDate = request.LastValuationDate;

        return newAsset;
    }

    /// <summary>
    ///     Converts a double money value to decimal, rounding to 2 decimal places
    ///     to avoid floating-point artifacts (e.g. 0.1 → 0.100000000000000005...).
    /// </summary>
    private static decimal ToMoney(double value) =>
        Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);

    /// <inheritdoc cref="ToMoney(double)"/>
    private static decimal? ToMoney(double? value) =>
        value.HasValue ? ToMoney(value.Value) : null;

    /// <summary>
    ///     Converts a decimal money value to double, rounding to 2 decimal places
    ///     to avoid floating-point representation artifacts in API responses.
    /// </summary>
    private static double FromMoney(decimal value) =>
        (double)Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <inheritdoc cref="FromMoney(decimal)"/>
    private static double? FromMoney(decimal? value) =>
        value.HasValue ? FromMoney(value.Value) : null;
}
