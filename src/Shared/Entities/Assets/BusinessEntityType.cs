namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Legal structure of a business entity.
/// </summary>
public enum BusinessEntityType
{
    /// <summary>Sole proprietorship (unincorporated single owner).</summary>
    SoleProprietorship,

    /// <summary>General or limited partnership.</summary>
    Partnership,

    /// <summary>Limited Liability Company.</summary>
    LLC,

    /// <summary>C-Corporation.</summary>
    Corporation,

    /// <summary>S-Corporation (pass-through tax treatment).</summary>
    SCorp,

    /// <summary>Other business entity type.</summary>
    Other
}
