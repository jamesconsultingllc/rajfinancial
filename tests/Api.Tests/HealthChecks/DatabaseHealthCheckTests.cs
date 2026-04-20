using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using RajFinancial.Api.Data;
using RajFinancial.Api.HealthChecks;

namespace RajFinancial.Api.Tests.HealthChecks;

/// <summary>
///     Unit tests for <see cref="DatabaseHealthCheck"/>.
/// </summary>
/// <remarks>
///     The reachable path is covered here via EF InMemory (always returns <c>true</c>
///     from <c>CanConnectAsync</c>). Exception branches (<see cref="System.Data.Common.DbException"/>,
///     <see cref="InvalidOperationException"/>) are intentionally covered by the
///     integration suite rather than unit-mocked — <c>DbContext.Database</c> is a
///     non-virtual sealed facade and faking the failure path without an integration
///     harness would require invasive seams that outweigh the coverage benefit.
/// </remarks>
public class DatabaseHealthCheckTests
{
    private static readonly HealthCheckContext ReadyContext = new()
    {
        Registration = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
            "database",
            instance: new NoopHealthCheck(),
            failureStatus: HealthStatus.Unhealthy,
            tags: null),
    };

    [Fact]
    public async Task Healthy_When_Database_Reachable()
    {
        await using var dbContext = CreateInMemoryContext();

        var sut = new DatabaseHealthCheck(dbContext, NullLogger<DatabaseHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database reachable");
    }

    [Fact]
    public async Task Healthy_Result_Ignores_FailureStatus_From_Registration()
    {
        // Sanity check: a Healthy result must always be Healthy regardless of the
        // registration's FailureStatus — FailureStatus only applies to the failure branches.
        await using var dbContext = CreateInMemoryContext();

        var sut = new DatabaseHealthCheck(dbContext, NullLogger<DatabaseHealthCheck>.Instance);

        var degradedContext = new HealthCheckContext
        {
            Registration = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                "database",
                instance: new NoopHealthCheck(),
                failureStatus: HealthStatus.Degraded,
                tags: null),
        };

        var result = await sut.CheckHealthAsync(degradedContext);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"db-health-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class NoopHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }
}
