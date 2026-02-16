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
using RajFinancial.Shared.Entities;

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
public class AssetFunctions(
    IAssetService assetService,
    ISerializationFactory serializationFactory,
    ILogger<AssetFunctions> logger)
{
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

        logger.LogInformation(
            "Fetching assets for owner {OwnerUserId} (requested by {UserId}, type={FilterType}, includeDisposed={IncludeDisposed})",
            ownerUserId, userId, filterType, includeDisposed);

        var assets = await assetService.GetAssetsAsync(userId, ownerUserId, filterType, includeDisposed);

        logger.LogInformation("Returned {Count} assets for owner {OwnerUserId}", assets.Count, ownerUserId);

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
            throw NotFoundException.Asset(Guid.Empty);

        logger.LogInformation("Fetching asset {AssetId} for user {UserId}", assetId, userId);

        var asset = await assetService.GetAssetByIdAsync(userId, assetId)
                    ?? throw NotFoundException.Asset(assetId);

        logger.LogInformation("Returned asset {AssetId} ({AssetName}) for user {UserId}",
            asset.Id, asset.Name, userId);

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

        logger.LogInformation("Creating asset for user {UserId}: {AssetName} ({AssetType})",
            userId, request.Name, request.Type);

        var asset = await assetService.CreateAssetAsync(userId, request);

        logger.LogInformation("Asset {AssetId} created for user {UserId}", asset.Id, userId);

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
            throw NotFoundException.Asset(Guid.Empty);

        var request = await context.GetValidatedBodyAsync<UpdateAssetRequest>();

        logger.LogInformation("Updating asset {AssetId} for user {UserId}: {AssetName} ({AssetType})",
            assetId, userId, request.Name, request.Type);

        var asset = await assetService.UpdateAssetAsync(userId, assetId, request);

        logger.LogInformation("Asset {AssetId} updated by user {UserId}", assetId, userId);

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
            throw NotFoundException.Asset(Guid.Empty);

        logger.LogInformation("Deleting asset {AssetId} for user {UserId}", assetId, userId);

        await assetService.DeleteAssetAsync(userId, assetId);

        logger.LogInformation("Asset {AssetId} deleted by user {UserId}", assetId, userId);

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

        if (Enum.TryParse<AssetType>(value, ignoreCase: true, out var parsed))
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
}
