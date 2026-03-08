namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Physical condition of a tangible asset (personal property, collectible).
/// </summary>
public enum ItemCondition
{
    /// <summary>Perfect, unused condition — as manufactured.</summary>
    Mint,

    /// <summary>Near-perfect with minimal signs of use.</summary>
    Excellent,

    /// <summary>Shows normal use but fully functional.</summary>
    Good,

    /// <summary>Noticeable wear or minor defects.</summary>
    Fair,

    /// <summary>Significant wear, damage, or defects.</summary>
    Poor
}
