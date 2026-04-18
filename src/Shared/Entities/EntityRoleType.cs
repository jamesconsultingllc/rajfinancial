namespace RajFinancial.Shared.Entities;

/// <summary>
///     Unified role type for business org chart and trust role assignments.
///     Business roles (0–9), Trust roles (10–19), Shared (99).
/// </summary>
public enum EntityRoleType
{
    // Business roles
    Owner = 0,
    Officer = 1,
    Director = 2,
    RegisteredAgent = 3,
    Employee = 4,
    Accountant = 5,
    Attorney = 6,

    // Trust roles
    Grantor = 10,
    Trustee = 11,
    SuccessorTrustee = 12,
    Beneficiary = 13,
    Protector = 14,
    TrustAdvisor = 15,

    // Shared
    Other = 99,
}
