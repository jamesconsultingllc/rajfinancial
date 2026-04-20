using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Pure role/entity compatibility and date-range rules. No database or DI.
/// </summary>
internal static class EntityRoleRules
{
    private static bool IsRoleCompatibleWithEntity(EntityRoleType roleType, EntityType entityType)
    {
        if (entityType == EntityType.Personal)
            return false;

        if (roleType == EntityRoleType.Other)
            return true;

        var code = (int)roleType;

        return entityType switch
        {
            EntityType.Business => code is >= 0 and <= 9,
            EntityType.Trust => code is >= 10 and <= 19,
            _ => false
        };
    }

    public static void ValidateRoleCompatibility(
        EntityRoleType roleType,
        EntityType entityType,
        CreateEntityRoleRequest request)
    {
        if (!IsRoleCompatibleWithEntity(roleType, entityType))
            throw new BusinessRuleException(
                EntityErrorCodes.RoleInvalidForEntityType,
                $"Role '{roleType}' is not valid for entity type '{entityType}'.");

        if (request.OwnershipPercent.HasValue && roleType != EntityRoleType.Owner)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleOwnershipNotAllowed,
                $"OwnershipPercent is only valid for Owner roles; received on '{roleType}'.");

        if (request.BeneficialInterestPercent.HasValue && roleType != EntityRoleType.Beneficiary)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleBeneficialInterestNotAllowed,
                $"BeneficialInterestPercent is only valid for Beneficiary roles; " +
                $"received on '{roleType}'.");
    }

    public static void ValidateRoleDateRange(DateTime? effectiveDate, DateTime? endDate)
    {
        if (effectiveDate.HasValue && endDate.HasValue && endDate.Value < effectiveDate.Value)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleDateRangeInvalid,
                "End date cannot precede effective date.");
    }
}
