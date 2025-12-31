using Microsoft.Playwright;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.Hooks;

/// <summary>
///     Extension methods for accessing Playwright objects from scenarioContext.
/// </summary>
public static class ScenarioContextExtensions
{
    public static IPage GetPage(this ScenarioContext context)
    {
        return context.Get<IPage>("Page");
    }

    public static IBrowserContext GetBrowserContext(this ScenarioContext context)
    {
        return context.Get<IBrowserContext>("BrowserContext");
    }
}