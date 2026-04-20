using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Constructs new <see cref="EntityRole"/> aggregates from request DTOs.
/// </summary>
internal static class EntityRoleFactory
{
    public static EntityRole Build(
        Guid entityId,
        EntityRoleType roleType,
        CreateEntityRoleRequest request) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        ContactId = request.ContactId,
        RoleType = roleType,
        Title = request.Title,
        OwnershipPercent = request.OwnershipPercent.HasValue
            ? (decimal)request.OwnershipPercent.Value
            : null,
        BeneficialInterestPercent = request.BeneficialInterestPercent.HasValue
            ? (decimal)request.BeneficialInterestPercent.Value
            : null,
        IsSignatory = request.IsSignatory,
        IsPrimary = request.IsPrimary,
        SortOrder = request.SortOrder,
        EffectiveDate = request.EffectiveDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.EffectiveDate.Value, DateTimeKind.Utc))
            : null,
        EndDate = request.EndDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc))
            : null,
        Notes = request.Notes
    };
}
