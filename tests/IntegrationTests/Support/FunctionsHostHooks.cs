using Reqnroll;
using Reqnroll.BoDi;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Reqnroll hook that manages the Functions host lifecycle.
/// Starts the host once before all tests and tears it down after all tests.
/// </summary>
[Binding]
public class FunctionsHostHooks
{
    private static FunctionsHostFixture? fixture;

    [BeforeTestRun]
    public static async Task StartHost(IObjectContainer container)
    {
        fixture = new FunctionsHostFixture();
        await fixture.InitializeAsync();
        container.RegisterInstanceAs(fixture);
    }

    [AfterTestRun]
    public static async Task StopHost()
    {
        if (fixture is not null)
        {
            await fixture.DisposeAsync();
            fixture = null;
        }
    }
}
