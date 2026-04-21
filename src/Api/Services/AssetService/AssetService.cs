// ============================================================================
// RAJ Financial - Asset Service Implementation
// ============================================================================
// EF Core implementation of IAssetService with three-tier authorization.
// Handles CRUD operations for Asset and DepreciableAsset entities,
// mapping to/from DTOs and computing depreciation at read time.
// ============================================================================

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Observability;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     EF Core implementation of <see cref="IAssetService"/>.
/// </summary>
public partial class AssetService(
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
        using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityGetList);
        activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
        if (filterType.HasValue)
            activity?.SetTag(AssetsTelemetry.TagAssetType, filterType.Value.ToString());

        try
        {
            await AuthorizeReadAsync(requestingUserId, ownerUserId);

            // Tag owner.user.id only after authorization succeeds so denied
            // requests don't leak the requested owner id (user-supplied) into
            // telemetry.
            activity?.SetTag(AssetsTelemetry.TagOwnerUserId, ownerUserId);

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

            activity?.SetTag(AssetsTelemetry.TagAssetsCount, assets.Count);
            return assets.Select(a => a.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task<AssetDetailDto?> GetAssetByIdAsync(Guid requestingUserId, Guid assetId)
    {
        using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityGetById);
        activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
        activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);

        try
        {
            var asset = await db.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset is null)
                return null;

            activity?.SetTag(AssetsTelemetry.TagAssetType, asset.Type.ToString());

            await AuthorizeReadAsync(requestingUserId, asset.UserId);

            activity?.SetTag(AssetsTelemetry.TagOwnerUserId, asset.UserId);

            return asset.ToDetailDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request)
    {
        using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityCreate);
        activity?.SetTag(AssetsTelemetry.TagUserId, userId);
        activity?.SetTag(AssetsTelemetry.TagOwnerUserId, userId);
        activity?.SetTag(AssetsTelemetry.TagAssetType, request.Type.ToString());

        try
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
                    CurrentValue = request.CurrentValue.ToMoney(),
                    PurchasePrice = request.PurchasePrice.ToMoney(),
                    PurchaseDate = request.PurchaseDate,
                    Description = request.Description,
                    Location = request.Location,
                    AccountNumber = request.AccountNumber,
                    InstitutionName = request.InstitutionName,
                    MarketValue = request.MarketValue.ToMoney(),
                    LastValuationDate = request.LastValuationDate,
                    CreatedAt = DateTimeOffset.UtcNow,
                    DepreciationMethod = request.DepreciationMethod!.Value,
                    SalvageValue = request.SalvageValue.ToMoney(),
                    UsefulLifeMonths = request.UsefulLifeMonths,
                    InServiceDate = request.InServiceDate
                }
                : new Asset
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = request.Name,
                    Type = request.Type,
                    CurrentValue = request.CurrentValue.ToMoney(),
                    PurchasePrice = request.PurchasePrice.ToMoney(),
                    PurchaseDate = request.PurchaseDate,
                    Description = request.Description,
                    Location = request.Location,
                    AccountNumber = request.AccountNumber,
                    InstitutionName = request.InstitutionName,
                    MarketValue = request.MarketValue.ToMoney(),
                    LastValuationDate = request.LastValuationDate,
                    CreatedAt = DateTimeOffset.UtcNow
                };

            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            activity?.SetTag(AssetsTelemetry.TagAssetId, asset.Id);
            AssetsTelemetry.RecordCreated(asset.Type.ToString());
            LogAssetCreated(asset.Id, userId);

            return asset.ToDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task<AssetDto> UpdateAssetAsync(Guid requestingUserId, Guid assetId, UpdateAssetRequest request)
    {
        using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityUpdate);
        activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
        activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);
        activity?.SetTag(AssetsTelemetry.TagAssetType, request.Type.ToString());

        try
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                        ?? throw NotFoundException.Asset(assetId);

            await AuthorizeWriteAsync(requestingUserId, asset.UserId);

            activity?.SetTag(AssetsTelemetry.TagOwnerUserId, asset.UserId);

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
                var newAsset = asset.Recreate(request, nowIsDepreciable);

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
                    LogAssetConcurrentTypeSwitch(ex, assetId);

                    throw new BusinessRuleException(
                        "ASSET_CONCURRENT_MODIFICATION",
                        $"Asset '{assetId}' was modified by another request. Please retry.");
                }

                LogAssetTypeChanged(assetId, nowIsDepreciable, asset.UserId);

                AssetsTelemetry.RecordUpdated(newAsset.Type.ToString(), typeSwitch: true);

                return newAsset.ToDto();
            }

            // Update common fields
            asset.Name = request.Name;
            asset.Type = request.Type;
            asset.CurrentValue = request.CurrentValue.ToMoney();
            asset.PurchasePrice = request.PurchasePrice.ToMoney();
            asset.PurchaseDate = request.PurchaseDate;
            asset.Description = request.Description;
            asset.Location = request.Location;
            asset.AccountNumber = request.AccountNumber;
            asset.InstitutionName = request.InstitutionName;
            asset.MarketValue = request.MarketValue.ToMoney();
            asset.LastValuationDate = request.LastValuationDate;
            asset.UpdatedAt = DateTimeOffset.UtcNow;

            // Update depreciation fields if applicable
            if (asset is DepreciableAsset depreciable && nowIsDepreciable)
            {
                depreciable.DepreciationMethod = request.DepreciationMethod!.Value;
                depreciable.SalvageValue = request.SalvageValue.ToMoney();
                depreciable.UsefulLifeMonths = request.UsefulLifeMonths;
                depreciable.InServiceDate = request.InServiceDate;
            }

            await db.SaveChangesAsync();

            AssetsTelemetry.RecordUpdated(asset.Type.ToString());
            LogAssetUpdated(assetId, requestingUserId);

            return asset.ToDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task DeleteAssetAsync(Guid requestingUserId, Guid assetId)
    {
        using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityDelete);
        activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
        activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);

        try
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                        ?? throw NotFoundException.Asset(assetId);

            activity?.SetTag(AssetsTelemetry.TagAssetType, asset.Type.ToString());

            await AuthorizeWriteAsync(requestingUserId, asset.UserId);

            activity?.SetTag(AssetsTelemetry.TagOwnerUserId, asset.UserId);

            db.Assets.Remove(asset);
            await db.SaveChangesAsync();

            AssetsTelemetry.RecordDeleted(asset.Type.ToString());
            LogAssetDeleted(assetId, requestingUserId);
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
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
    // Source-generated logging (EventId 2000-2999)
    // =========================================================================

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Asset {AssetId} created for user {UserId}")]
    private partial void LogAssetCreated(Guid assetId, Guid userId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Asset {AssetId} updated by user {UserId}")]
    private partial void LogAssetUpdated(Guid assetId, Guid userId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Asset {AssetId} deleted by user {UserId}")]
    private partial void LogAssetDeleted(Guid assetId, Guid userId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Asset {AssetId} type changed (depreciable={IsDepreciable}) for user {UserId}")]
    private partial void LogAssetTypeChanged(Guid assetId, bool isDepreciable, Guid userId);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Warning,
        Message = "Concurrent type-switch conflict for asset {AssetId}. Client should retry.")]
    private partial void LogAssetConcurrentTypeSwitch(Exception exception, Guid assetId);
}
