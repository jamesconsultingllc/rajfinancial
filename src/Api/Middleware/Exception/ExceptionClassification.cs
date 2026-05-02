using Microsoft.EntityFrameworkCore;

namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Single source of truth for classifying exceptions as handled
///     client errors (4xx) vs server errors (5xx). Used by
///     <c>ActivityExceptionExtensions.RecordExceptionOutcome</c> to decide
///     whether an exception should flip the OTel span status to ERROR, and
///     kept structurally aligned with the <c>catch</c>/switch arms in
///     <see cref="ExceptionMiddleware" />.
/// </summary>
/// <remarks>
///     When adding a new <c>catch</c> block or switch arm to
///     <see cref="ExceptionMiddleware" /> that maps to a 4xx status, also add
///     the exception type here so OTel spans record it as a client-error
///     outcome (event only, status Unset) instead of flagging it as a server
///     error. The <c>ExceptionClassificationTests</c> in the Api.Tests project
///     lock this in.
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
        RateLimitedException or
        DbUpdateConcurrencyException;
}
