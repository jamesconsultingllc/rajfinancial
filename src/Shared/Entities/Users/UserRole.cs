// ============================================================================
// RAJ Financial - User Roles
// ============================================================================
// This enum defines the roles used within the RAJ Financial system. 
// ============================================================================

namespace RajFinancial.Shared.Entities.Users;

/// <summary>
///     Defines the roles a user can have in the RAJ Financial system.
/// </summary>
public enum UserRole
{
    /// <summary>
    ///     System administrator with full access to all features.
    /// </summary>
    Administrator = 0,

    /// <summary>
    ///     Financial advisor who manages client portfolios.
    /// </summary>
    Advisor = 1,

    /// <summary>
    ///     Client user who views their own financial data.
    /// </summary>
    Client = 2
}