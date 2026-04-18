namespace RajFinancial.Shared.Entities;

/// <summary>
///     Motivations for creating a trust. Multiple goals may apply to a single
///     trust, so this is a [Flags] enum that can be OR'd together.
/// </summary>
[Flags]
public enum TrustGoal
{
    None = 0,
    AvoidProbate = 1 << 0,
    Privacy = 1 << 1,
    ProtectChildren = 1 << 2,
    AvoidFamilyFights = 1 << 3,
    ProtectSpouse = 1 << 4,
    ControlDistribution = 1 << 5,
    ReduceTaxes = 1 << 6,
    ProtectAssets = 1 << 7,
}
