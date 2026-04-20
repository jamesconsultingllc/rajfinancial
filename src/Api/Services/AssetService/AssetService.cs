// ============================================================================
// RAJ Financial - Asset Service Implementation
// ============================================================================
// EF Core implementation of IAssetService with three-tier authorization.
// Handles CRUD operations for Asset and DepreciableAsset entities,
// mapping to/from DTOs and computing depreciation at read time.
// ============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
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
    private static readonly ActivitySource ActivitySource = new("RajFinancial.Api.Assets");
    private static readonly Meter Meter = new("RajFinancial.Api.Assets");

    private static readonly Counter<long> AssetsCreated =
        Meter.CreateCounter<long>("assets.created.count");

    private static readonly Counter<long> AssetsUpdated =
        Meter.CreateCounter<long>("assets.updated.count");

    private static readonly Counter<long> AssetsDeleted =
        Meter.CreateCounter<long>("assets.deleted.count");

    private static readonly Histogram<double> AssetsQueryDuration =
        Meter.CreateHistogram<double>("assets.query.duration.ms");

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        AssetType? filterType = null,
        bool includeDisposed = false)
    {
        using var activity = ActivitySource.StartActivity("Assets.GetList");
        activity?.SetTag("user.id", requestingUserId);
        activity?.SetTag("owner.user.id", ownerUserId);
        if (filterType.HasValue)
            activity?.SetTag("asset.type", filterType.Value.ToString());

        var sw = Stopwatch.StartNew();
        try
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

            activity?.SetTag("assets.count", assets.Count);
            return assets.Select(a => a.ToDto()).ToList();
        }
        finally
        {
            AssetsQueryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "list"));
        }
    }

    public async Task<AssetDetailDto?> GetAssetByIdAsync(Guid requestingUserId, Guid assetId)
    {
        using var activity = ActivitySource.StartActivity("Assets.GetById");
        activity?.SetTag("user.id", requestingUserId);
        activity?.SetTag("asset.id", assetId);

        var sw = Stopwatch.StartNew();
        try
        {
            var asset = await db.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset is null)
                return null;

            activity?.SetTag("asset.type", asset.Type.ToString());

            await AuthorizeReadAsync(requestingUserId, asset.UserId);

            return asset.ToDetailDto();
        }
        finally
        {
            AssetsQueryDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "get"));
        }
    }

    public async Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request)
    {
        using var activity = ActivitySource.StartActivity("Assets.Create");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("asset.type", request.Type.ToString());

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

        activity?.SetTag("asset.id", asset.Id);
        AssetsCreated.Add(1,
            new KeyValuePair<string, object?>("asset.type", asset.Type.ToString()));
        LogAssetCreated(asset.Id, userId);

        return asset.ToDto();
    }

    public async Task<AssetDto> UpdateAssetAsync(Guid requestingUserId, Guid assetId, UpdateAssetRequest request)
    {
        using var activity = ActivitySource.StartActivity("Assets.Update");
        activity?.SetTag("user.id", requestingUserId);
        activity?.SetTag("asset.id", assetId);
        activity?.SetTag("asset.type", request.Type.ToString());

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

            AssetsUpdated.Add(1,
                new KeyValuePair<string, object?>("asset.type", newAsset.Type.ToString()),
                new KeyValuePair<string, object?>("type_switch", true));

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

        AssetsUpdated.Add(1,
            new KeyValuePair<string, object?>("asset.type", asset.Type.ToString()));
        LogAssetUpdated(assetId, requestingUserId);

        return asset.ToDto();
    }

    public async Task DeleteAssetAsync(Guid requestingUserId, Guid assetId)
    {
        using var activity = ActivitySource.StartActivity("Assets.Delete");
        activity?.SetTag("user.id", requestingUserId);
        activity?.SetTag("asset.id", assetId);

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                    ?? throw NotFoundException.Asset(assetId);

        activity?.SetTag("asset.type", asset.Type.ToString());

        await AuthorizeWriteAsync(requestingUserId, asset.UserId);

        db.Assets.Remove(asset);
        await db.SaveChangesAsync();

        AssetsDeleted.Add(1,
            new KeyValuePair<string, object?>("asset.type", asset.Type.ToString()));
        LogAssetDeleted(assetId, requestingUserId);
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
