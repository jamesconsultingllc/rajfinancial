using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Unified role type for business org chart and trust role assignments.
///     Business roles (0–9), Trust roles (10–19), Shared (99).
/// </summary>
public enum EntityRoleType
{
    // Business roles
    [Description("Owner — equity holder (member, shareholder, partner).")]
    Owner = 0,

    [Description("Officer — executive officer (CEO, CFO, President, Secretary, Treasurer).")]
    Officer = 1,

    [Description("Director — member of the board of directors.")]
    Director = 2,

    [Description("Registered Agent — designated agent for service of process in the state of formation.")]
    RegisteredAgent = 3,

    [Description("Employee — worker employed by the business entity.")]
    Employee = 4,

    [Description("Accountant — CPA or bookkeeper responsible for financial records.")]
    Accountant = 5,

    [Description("Attorney — legal counsel for the entity.")]
    Attorney = 6,

    // Trust roles
    [Description("Grantor — the person who creates and funds the trust (a.k.a. settlor or trustor).")]
    Grantor = 10,

    [Description("Trustee — individual or institution that holds and administers trust assets.")]
    Trustee = 11,

    [Description("Successor Trustee — takes over if the primary trustee dies, resigns, or is removed.")]
    SuccessorTrustee = 12,

    [Description("Beneficiary — person or entity entitled to benefit from the trust.")]
    Beneficiary = 13,

    [Description("Trust Protector — third party with limited powers to oversee or amend the trust.")]
    Protector = 14,

    [Description("Trust Advisor — provides investment, distribution, or other guidance to the trustee.")]
    TrustAdvisor = 15,

    // Shared
    [Description("Other — role not covered by the predefined list.")]
    Other = 99,
}