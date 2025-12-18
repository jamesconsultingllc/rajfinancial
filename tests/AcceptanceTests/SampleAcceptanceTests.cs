using Microsoft.Playwright;

namespace RajFinancial.AcceptanceTests;

public class SampleAcceptanceTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl;

    public SampleAcceptanceTests()
    {
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:4280";
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_ShouldLoad()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        // Act
        var response = await page.GotoAsync(_baseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Failed to load page: {response.Status} {response.StatusText}");

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_ShouldHaveTitle()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        // Act
        await page.GotoAsync(_baseUrl);
        var title = await page.TitleAsync();

        // Assert
        Assert.NotNull(title);
        Assert.NotEmpty(title);

        await page.CloseAsync();
    }
}

