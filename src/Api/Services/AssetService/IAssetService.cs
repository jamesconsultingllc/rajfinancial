using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Provides CRUD operations for user-owned assets with authorization enforcement.
/// </summary>
/// <remarks>
///     <para>
///         All operations enforce the three-tier authorization model via
///         <see cref="Authorization.IAuthorizationService"/>:
///         <list type="number">
///             <item><b>Owner:</b> The user owns the asset.</item>
///             <item><b>DataAccessGrant:</b> Another user has been granted access to the owner's assets.</item>
///             <item><b>Administrator:</b> Platform administrators have full access.</item>
///         </list>
///     </para>
///     <para>
///         <b>Depreciation:</b> Computed at read time by <see cref="DepreciationCalculator"/>,
///         not stored in the database. The <see cref="AssetDetailDto"/> includes computed fields
///         such as <c>AccumulatedDepreciation</c>, <c>BookValue</c>, and <c>MonthlyDepreciation</c>.
///     </para>
///     <para>
///         <b>Data Category:</b> <see cref="DataCategories.Assets"/>
///     </para>
/// </remarks>
public interface IAssetService
{
    /// <summary>
    ///     Retrieves all assets for a user, optionally filtered by asset type.
    /// </summary>
    /// <param name="requestingUserId">The Entra Object ID of the user making the request.</param>
    /// <param name="ownerUserId">The Entra Object ID of the asset owner.</param>
    /// <param name="filterType">Optional asset type filter. Null returns all types.</param>
    /// <param name="includeDisposed">Whether to include disposed assets. Defaults to false.</param>
    /// <returns>A collection of asset summary DTOs.</returns>
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        AssetType? filterType = null,
        bool includeDisposed = false);

    /// <summary>
    ///     Retrieves a single asset by ID with full details including depreciation and beneficiaries.
    /// </summary>
    /// <param name="requestingUserId">The Entra Object ID of the user making the request.</param>
    /// <param name="assetId">The unique identifier of the asset.</param>
    /// <returns>The detailed asset DTO, or null if not found.</returns>
    Task<AssetDetailDto?> GetAssetByIdAsync(Guid requestingUserId, Guid assetId);

    /// <summary>
    ///     Creates a new asset for the authenticated user.
    /// </summary>
    /// <remarks>
    ///     Only the asset owner can create assets — advisors and administrators
    ///     cannot create assets on behalf of another user. The <paramref name="userId"/>
    ///     is both the requesting user and the owner.
    /// </remarks>
    /// <param name="userId">The Entra Object ID of the authenticated user (owner).</param>
    /// <param name="request">The asset creation request.</param>
    /// <returns>The created asset DTO with generated ID.</returns>
    Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request);

    /// <summary>
    ///     Updates an existing asset. Validates user ownership before applying changes.
    /// </summary>
    /// <param name="requestingUserId">The Entra Object ID of the user making the request.</param>
    /// <param name="assetId">The unique identifier of the asset to update.</param>
    /// <param name="request">The updated asset data.</param>
    /// <returns>The updated asset DTO.</returns>
    Task<AssetDto> UpdateAssetAsync(Guid requestingUserId, Guid assetId, UpdateAssetRequest request);

    /// <summary>
    ///     Deletes an asset. Removes beneficiary assignments first, then deletes the asset.
    ///     Validates user ownership before deletion.
    /// </summary>
    /// <param name="requestingUserId">The Entra Object ID of the user making the request.</param>
    /// <param name="assetId">The unique identifier of the asset to delete.</param>
    Task DeleteAssetAsync(Guid requestingUserId, Guid assetId);
}
