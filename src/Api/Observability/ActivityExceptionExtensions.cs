// ============================================================================
// RAJ Financial - Activity Exception Extensions
// ============================================================================
// Helper for recording exceptions on OpenTelemetry activities with correct
// status classification per W3C/OTel HTTP semantic conventions:
//   - 4xx handled client errors (validation, not-found, auth, business-rule)
//     are recorded as exception events but do NOT mark the span as Error,
//     because they are intentional outcomes mapped to HTTP responses by
//     ExceptionMiddleware, not server failures.
//   - 5xx-class / unexpected exceptions mark the span as Error and record
//     the exception event so traces surface in error-rate telemetry.
//   - OperationCanceledException (cooperative cancellation) is neither
//     recorded nor flagged as an error.
// ============================================================================

using System.Diagnostics;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Observability;

internal static class ActivityExceptionExtensions
{
    /// <summary>
    ///     Records <paramref name="ex" /> on <paramref name="activity" />
    ///     following W3C/OTel HTTP span conventions.
    /// </summary>
    public static void RecordExceptionOutcome(this Activity activity, Exception ex)
    {
        if (ex is OperationCanceledException)
            return;

        activity.AddException(ex);

        if (IsHandledClientException(ex))
            return;

        activity.SetStatus(ActivityStatusCode.Error);
    }

    private static bool IsHandledClientException(Exception ex) => ex is
        ValidationException or
        NotFoundException or
        ForbiddenException or
        UnauthorizedException or
        ConflictException or
        BusinessRuleException;
}
