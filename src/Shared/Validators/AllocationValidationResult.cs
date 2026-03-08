namespace RajFinancial.Shared.Validators;

/// <summary>
///     Describes a single allocation validation error.
/// </summary>
/// <param name="ErrorCode">Machine-readable error code from <see cref="Contracts.Assets.AllocationErrorCodes"/>.</param>
/// <param name="Message">Human-readable error message.</param>
public sealed record AllocationValidationError(string ErrorCode, string Message);

/// <summary>
///     Result of validating a set of beneficiary allocation assignments.
/// </summary>
public sealed record AllocationValidationResult
{
    /// <summary>Sum of primary beneficiary allocations.</summary>
    public double PrimaryTotal { get; init; }

    /// <summary>Sum of contingent beneficiary allocations.</summary>
    public double ContingentTotal { get; init; }

    /// <summary>Whether primary allocations total exactly 100%.</summary>
    public bool IsPrimaryValid { get; init; }

    /// <summary>Whether contingent allocations total exactly 100% (or no contingent beneficiaries exist).</summary>
    public bool IsContingentValid { get; init; }

    /// <summary>All validation errors found.</summary>
    public IReadOnlyList<AllocationValidationError> Errors { get; init; } = [];

    /// <summary>True when all allocation rules pass.</summary>
    public bool IsValid => Errors.Count == 0;
}
