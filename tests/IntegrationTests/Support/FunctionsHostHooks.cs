using Reqnroll;
using Reqnroll.BoDi;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Reqnroll hook that registers the Functions host fixture for DI.
/// The host must already be running — this does NOT auto-start it.
/// </summary>
[Binding]
public class FunctionsHostHooks
{
    [BeforeTestRun]
    public static void RegisterFixture(IObjectContainer container)
    {
        var fixture = new FunctionsHostFixture();
        container.RegisterInstanceAs(fixture);
    }
}
