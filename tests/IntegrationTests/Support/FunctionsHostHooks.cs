using Reqnroll;
using Reqnroll.BoDi;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Reqnroll hook that registers shared test infrastructure for DI.
/// The Functions host must already be running — this does NOT auto-start it.
/// </summary>
[Binding]
public static class FunctionsHostHooks
{
    [BeforeTestRun]
    public static void RegisterFixture(IObjectContainer container)
    {
        var fixture = new FunctionsHostFixture();
        container.RegisterInstanceAs(fixture);

        var ropcProvider = new RopcTokenProvider(fixture.Configuration);
        container.RegisterInstanceAs(ropcProvider);
        container.RegisterInstanceAs(new TestAuthHelper(fixture, ropcProvider, fixture.Configuration));
    }
}
