using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Tests.Middleware.ExceptionHandling;

/// <summary>
///     Locks in the <see cref="ExceptionClassification.IsHandledClientException" />
///     whitelist. When a new exception type is mapped to a 4xx in
///     <see cref="ExceptionMiddleware" />, add it to the classifier AND here so
///     drift is caught by CI.
/// </summary>
public class ExceptionClassificationTests
{
    public static TheoryData<System.Exception> HandledClientExceptions() => new()
    {
        new ValidationException("v"),
        new NotFoundException("NOT_FOUND", "nf"),
        new ForbiddenException("f"),
        new UnauthorizedException("u"),
        new ConflictException("CONFLICT", "c"),
        new BusinessRuleException("RULE", "b"),
        new DbUpdateConcurrencyException(),
    };

    public static TheoryData<System.Exception> ServerExceptions() => new()
    {
        new InvalidOperationException("boom"),
        new NullReferenceException("nre"),
        new ApplicationException("app"),
        new DbUpdateException("db"),
    };

    [Theory]
    [MemberData(nameof(HandledClientExceptions))]
    public void IsHandledClientException_ReturnsTrue_ForClientErrors(System.Exception ex)
    {
        ExceptionClassification.IsHandledClientException(ex).Should().BeTrue(
            "{0} is translated to a 4xx by ExceptionMiddleware and must not flip the span to ERROR",
            ex.GetType().Name);
    }

    [Theory]
    [MemberData(nameof(ServerExceptions))]
    public void IsHandledClientException_ReturnsFalse_ForServerErrors(System.Exception ex)
    {
        ExceptionClassification.IsHandledClientException(ex).Should().BeFalse(
            "{0} is a server error and should flip the OTel span to ERROR status",
            ex.GetType().Name);
    }
}
