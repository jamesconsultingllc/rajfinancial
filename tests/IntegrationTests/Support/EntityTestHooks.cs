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
    [Scope(Tag = "entity-roles")]
    public async Task CleanupBeforeEntityScenarioAsync()
    {
        await EntityTestDataCleanup.CleanupAsync(fixture.Configuration);
    }

    [BeforeScenario(Order = 11)]
    [Scope(Tag = "entity-roles")]
    public async Task ResetSeededContactsAsync()
    {
        // The SeedableContactResolver is process-scoped: contacts seeded by a prior
        // scenario remain in memory and could mask "this contactId is not seeded"
        // assertions in subsequent scenarios. Reset between role scenarios so
        // each test starts from an empty contact map.
        await ContactSeedingHelper.ResetAsync(fixture.Client);
    }
}
