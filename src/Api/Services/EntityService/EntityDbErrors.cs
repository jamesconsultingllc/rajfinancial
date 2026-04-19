using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Database-level error helpers for the Entity aggregate — factoring <see cref="NotFoundException"/>
///     construction and SQL Server unique-constraint detection.
/// </summary>
internal static class EntityDbErrors
{
    // SQL Server error numbers:
    //   2601 — Cannot insert duplicate key row in object with unique index.
    //   2627 — Violation of UNIQUE KEY / PRIMARY KEY constraint.
    // Both indicate a unique-index/key collision on the underlying table.
    private const int SqlErrorDuplicateKey = 2601;
    private const int SqlErrorUniqueConstraint = 2627;

    public static NotFoundException NotFound(Guid entityId) =>
        new(EntityErrorCodes.NotFound, $"Entity with ID {entityId} was not found.");

    public static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && (sql.Number == SqlErrorDuplicateKey || sql.Number == SqlErrorUniqueConstraint);
}
