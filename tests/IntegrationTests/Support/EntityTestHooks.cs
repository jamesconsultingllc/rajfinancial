using Reqnroll;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Reqnroll hooks that keep entity-related tests isolated across runs by
/// wiping residual test data before each scenario tagged <c>@entities</c>
/// or <c>@entity-roles</c>.
/// </summary>
[Binding]
public sealed class EntityTestHooks(FunctionsHostFixture fixture)
{
    [BeforeScenario(Order = 10)]
    [Scope(Tag = "entities")]
    public async Task CleanupBeforeEntityScenarioAsync()
    {
        await EntityTestDataCleanup.CleanupAsync(fixture.Configuration);
    }

    [BeforeScenario(Order = 10)]
    [Scope(Tag = "entity-roles")]
    public async Task CleanupBeforeEntityRoleScenarioAsync()
    {
        await EntityTestDataCleanup.CleanupAsync(fixture.Configuration);
    }
}
