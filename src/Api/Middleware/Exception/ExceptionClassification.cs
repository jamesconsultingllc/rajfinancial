using Microsoft.EntityFrameworkCore;

namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Single source of truth for classifying exceptions as handled
///     client errors (4xx) vs server errors (5xx). Used by
///     <see cref="ExceptionMiddleware" /> (for asserts/tests) and by
///     <c>ActivityExceptionExtensions</c> (for span status classification).
/// </summary>
/// <remarks>
///     When adding a new <c>catch</c> block to <see cref="ExceptionMiddleware" />
///     that maps to a 4xx status, also add the exception type here so OTel
///     spans record it as a client-error outcome (event only, status Unset)
///     instead of flagging it as a server error.
/// </remarks>
internal static class ExceptionClassification
{
    /// <summary>
    ///     Returns <c>true</c> if <paramref name="ex" /> is translated to a
    ///     4xx response by <see cref="ExceptionMiddleware" />.
    /// </summary>
    public static bool IsHandledClientException(System.Exception ex) => ex is
        ValidationException or
        NotFoundException or
        ForbiddenException or
        UnauthorizedException or
        ConflictException or
        BusinessRuleException or
        DbUpdateConcurrencyException;
}
