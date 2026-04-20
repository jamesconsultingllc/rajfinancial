using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.AssetService;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Functions.Assets;

/// <summary>
///     Azure Function endpoints for asset CRUD operations.
/// </summary>
/// <remarks>
///     <para>
///         <b>Route prefix:</b> <c>/api/assets</c>
///     </para>
///     <para>
///         <b>Authorization:</b> All endpoints require authentication. Three-tier authorization
///         (owner → DataAccessGrant → administrator) is enforced by <see cref="IAssetService"/>.
///     </para>
///     <para>
///         <b>Endpoints:</b>
///         <list type="bullet">
///             <item><c>GET    /api/assets</c>      — List assets with optional filters</item>
///             <item><c>GET    /api/assets/{id}</c>  — Get asset detail by ID</item>
///             <item><c>POST   /api/assets</c>      — Create a new asset</item>
///             <item><c>PUT    /api/assets/{id}</c>  — Update an existing asset</item>
///             <item><c>DELETE /api/assets/{id}</c>  — Delete an asset</item>
///         </list>
///     </para>
/// </remarks>
public partial class AssetFunctions(
    IAssetService assetService,
    ISerializationFactory serializationFactory,
    ILogger<AssetFunctions> logger)
{
    private static readonly ActivitySource ActivitySource = new("RajFinancial.Api.Assets");

    // =========================================================================
    // GET /api/assets
    // =========================================================================

    /// <summary>
    ///     Retrieves all assets for a user, with optional filtering.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Query Parameters:</b>
    ///         <list type="bullet">
    ///             <item><c>ownerUserId</c> — Optional. The user whose assets to retrieve. Defaults to the authenticated user.</item>
    ///             <item><c>type</c> — Optional. Filter by <see cref="AssetType"/> (integer or string value).</item>
    ///             <item><c>includeDisposed</c> — Optional. Include disposed assets. Defaults to <c>false</c>.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Response:</b> <c>200 OK</c> with <c>IReadOnlyList&lt;AssetDto&gt;</c>
    ///     </para>
    /// </remarks>
    [RequireAuthentication]
    [Function("GetAssets")]
    public async Task<HttpResponseData> GetAssets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var ownerUserId = ParseOwnerUserId(req) ?? userId;
        var filterType = ParseAssetType(req);
        var includeDisposed = ParseBool(req, "includeDisposed");

        using var activity = ActivitySource.StartActivity("Assets.Http.GetList");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("owner.user.id", ownerUserId);
        if (filterType.HasValue)
            activity?.SetTag("asset.type", filterType.Value.ToString());

        LogFetchingAssets(ownerUserId, userId, filterType, includeDisposed);

        var assets = await assetService.GetAssetsAsync(userId, ownerUserId, filterType, includeDisposed);

        activity?.SetTag("assets.count", assets.Count);
        LogAssetsReturned(assets.Count, ownerUserId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, assets, serializationFactory);
    }

    // =========================================================================
    // GET /api/assets/{id}
    // =========================================================================

    /// <summary>
    ///     Retrieves a single asset by ID with full details including depreciation,
    ///     disposal info, and beneficiary assignments.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Response:</b> <c>200 OK</c> with <see cref="AssetDetailDto"/>
    ///     </para>
    ///     <para>
    ///         <b>Error:</b> <c>404 Not Found</c> if the asset does not exist.
    ///     </para>
    /// </remarks>
    [RequireAuthentication]
    [Function("GetAssetById")]
    public async Task<HttpResponseData> GetAssetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var assetId))
            throw new ValidationException($"Invalid asset ID format: '{id}'");

        using var activity = ActivitySource.StartActivity("Assets.Http.GetById");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("asset.id", assetId);

        LogFetchingAssetById(assetId, userId);

        var asset = await assetService.GetAssetByIdAsync(userId, assetId)
                    ?? throw NotFoundException.Asset(assetId);

        activity?.SetTag("asset.type", asset.Type.ToString());
        LogAssetByIdReturned(asset.Id, asset.Name, userId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, asset, serializationFactory);
    }

    // =========================================================================
    // POST /api/assets
    // =========================================================================

    /// <summary>
    ///     Creates a new asset for the authenticated user.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Request:</b> <see cref="CreateAssetRequest"/> (validated by
    ///         <see cref="Validators.CreateAssetRequestValidator"/>)
    ///     </para>
    ///     <para>
    ///         <b>Response:</b> <c>201 Created</c> with <see cref="AssetDto"/>
    ///     </para>
    /// </remarks>
    [RequireAuthentication]
    [Function("CreateAsset")]
    public async Task<HttpResponseData> CreateAsset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assets")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var request = await context.GetValidatedBodyAsync<CreateAssetRequest>();

        using var activity = ActivitySource.StartActivity("Assets.Http.Create");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("asset.type", request.Type.ToString());

        LogCreatingAsset(userId, request.Name, request.Type);

        var asset = await assetService.CreateAssetAsync(userId, request);

        activity?.SetTag("asset.id", asset.Id);
        LogAssetCreated(asset.Id, userId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.Created, asset, serializationFactory);
    }

    // =========================================================================
    // PUT /api/assets/{id}
    // =========================================================================

    /// <summary>
    ///     Updates an existing asset.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Request:</b> <see cref="UpdateAssetRequest"/> (validated by
    ///         <see cref="Validators.UpdateAssetRequestValidator"/>)
    ///     </para>
    ///     <para>
    ///         <b>Response:</b> <c>200 OK</c> with updated <see cref="AssetDto"/>
    ///     </para>
    ///     <para>
    ///         <b>Error:</b> <c>404 Not Found</c> if the asset does not exist.
    ///     </para>
    /// </remarks>
    [RequireAuthentication]
    [Function("UpdateAsset")]
    public async Task<HttpResponseData> UpdateAsset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "assets/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var assetId))
            throw new ValidationException($"Invalid asset ID format: '{id}'");

        var request = await context.GetValidatedBodyAsync<UpdateAssetRequest>();

        using var activity = ActivitySource.StartActivity("Assets.Http.Update");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("asset.id", assetId);
        activity?.SetTag("asset.type", request.Type.ToString());

        LogUpdatingAsset(assetId, userId, request.Name, request.Type);

        var asset = await assetService.UpdateAssetAsync(userId, assetId, request);

        LogAssetUpdated(assetId, userId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, asset, serializationFactory);
    }

    // =========================================================================
    // DELETE /api/assets/{id}
    // =========================================================================

    /// <summary>
    ///     Deletes an asset by ID. Beneficiary assignments are removed first,
    ///     then the asset is hard-deleted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Response:</b> <c>204 No Content</c>
    ///     </para>
    ///     <para>
    ///         <b>Error:</b> <c>404 Not Found</c> if the asset does not exist.
    ///     </para>
    /// </remarks>
    [RequireAuthentication]
    [Function("DeleteAsset")]
    public async Task<HttpResponseData> DeleteAsset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "assets/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var assetId))
            throw new ValidationException($"Invalid asset ID format: '{id}'");

        using var activity = ActivitySource.StartActivity("Assets.Http.Delete");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("asset.id", assetId);

        LogDeletingAsset(assetId, userId);

        await assetService.DeleteAssetAsync(userId, assetId);

        LogAssetDeleted(assetId, userId);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // Query Parameter Helpers
    // =========================================================================

    /// <summary>
    ///     Parses the <c>ownerUserId</c> query parameter.
    /// </summary>
    private static Guid? ParseOwnerUserId(HttpRequestData req)
    {
        var value = req.Query["ownerUserId"];
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    /// <summary>
    ///     Parses the <c>type</c> query parameter as an <see cref="AssetType"/> enum.
    /// </summary>
    private static AssetType? ParseAssetType(HttpRequestData req)
    {
        var value = req.Query["type"];
        if (value is null) return null;

        // Support both integer and string enum values
        if (int.TryParse(value, out var intValue) && Enum.IsDefined(typeof(AssetType), intValue))
            return (AssetType)intValue;

        if (Enum.TryParse<AssetType>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            return parsed;

        return null;
    }

    /// <summary>
    ///     Parses a boolean query parameter. Returns false if not present or not parseable.
    /// </summary>
    private static bool ParseBool(HttpRequestData req, string name)
    {
        var value = req.Query[name];
        return bool.TryParse(value, out var parsed) && parsed;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Fetching assets for owner {OwnerUserId} (requested by {UserId}, type={FilterType}, includeDisposed={IncludeDisposed})")]
    private partial void LogFetchingAssets(Guid ownerUserId, Guid userId, AssetType? filterType, bool includeDisposed);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Returned {Count} assets for owner {OwnerUserId}")]
    private partial void LogAssetsReturned(int count, Guid ownerUserId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Fetching asset {AssetId} for user {UserId}")]
    private partial void LogFetchingAssetById(Guid assetId, Guid userId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Returned asset {AssetId} ({AssetName}) for user {UserId}")]
    private partial void LogAssetByIdReturned(Guid assetId, string assetName, Guid userId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Creating asset for user {UserId}: {AssetName} ({AssetType})")]
    private partial void LogCreatingAsset(Guid userId, string assetName, AssetType assetType);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Asset {AssetId} created for user {UserId}")]
    private partial void LogAssetCreated(Guid assetId, Guid userId);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information, Message = "Updating asset {AssetId} for user {UserId}: {AssetName} ({AssetType})")]
    private partial void LogUpdatingAsset(Guid assetId, Guid userId, string assetName, AssetType assetType);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Asset {AssetId} updated by user {UserId}")]
    private partial void LogAssetUpdated(Guid assetId, Guid userId);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Information, Message = "Deleting asset {AssetId} for user {UserId}")]
    private partial void LogDeletingAsset(Guid assetId, Guid userId);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Asset {AssetId} deleted by user {UserId}")]
    private partial void LogAssetDeleted(Guid assetId, Guid userId);
}
