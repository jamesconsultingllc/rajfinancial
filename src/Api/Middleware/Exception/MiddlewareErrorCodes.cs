namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Stable error codes returned by <see cref="ExceptionMiddleware" />
///     for cross-cutting failures that don't belong to a specific domain.
///     Domain-specific codes live in <c>*ErrorCodes</c> classes under
///     <c>RajFinancial.Shared.Contracts.*</c>.
/// </summary>
public static class MiddlewareErrorCodes
{
    /// <summary>400 - Request body/query/route validation failed.</summary>
    public const string ValidationFailed = "VALIDATION_FAILED";

    /// <summary>401 - No authenticated principal on the request.</summary>
    public const string AuthRequired = "AUTH_REQUIRED";

    /// <summary>403 - Authenticated principal lacks required permissions.</summary>
    public const string AuthForbidden = "AUTH_FORBIDDEN";

    /// <summary>409 - Optimistic concurrency failure during SaveChanges.</summary>
    public const string DbConcurrencyConflict = "DB_CONCURRENCY_CONFLICT";

    /// <summary>500 - Service misconfiguration detected at request time.</summary>
    public const string ConfigurationError = "CONFIGURATION_ERROR";

    /// <summary>500 - Unhandled exception escaped the middleware chain.</summary>
    public const string InternalError = "INTERNAL_ERROR";
}
