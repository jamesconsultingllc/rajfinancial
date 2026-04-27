# Raj Financial Test Suite

This directory contains the test projects for Raj Financial. The tests are organized into three categories following best practices for .NET applications.

## Test Projects

### 1. Unit (`tests/Api.Tests/`)

**Purpose**: Fast, isolated tests that verify individual components, services, and business logic.

**Framework**: xUnit with .NET 10

**Characteristics**:
- No external dependencies (databases, APIs, file system)
- Fast execution (milliseconds per test)
- High test coverage
- Run in CI pipeline before deployment

**Example Test**:
```csharp
[Fact]
public void Calculator_Add_ReturnsCorrectSum()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    var result = calculator.Add(2, 3);
    
    // Assert
    Assert.Equal(5, result);
}
```

**When to Add Tests**:
- New business logic
- New utility methods
- Data transformations
- Validation logic
- Pure functions

### 2. IntegrationTests (`tests/IntegrationTests/`)

**Purpose**: Test interactions between components, including database access, API calls, and external services.

**Framework**: xUnit with .NET 10

**Characteristics**:
- Tests multiple components working together
- May use test databases or mocked services
- Slower than unit tests (seconds per test)
- Verifies integration points

**Example Test**:
```csharp
[Fact]
public async Task UserRepository_SaveUser_PersistsToDatabase()
{
    // Arrange
    var repository = new UserRepository(testDbContext);
    var user = new User { Name = "Test User" };
    
    // Act
    await repository.SaveAsync(user);
    
    // Assert
    var savedUser = await repository.GetByIdAsync(user.Id);
    Assert.NotNull(savedUser);
    Assert.Equal("Test User", savedUser.Name);
}
```

**When to Add Tests**:
- Database operations
- API endpoint integration
- Authentication/authorization flows
- Third-party service integration
- Configuration validation

### 3. End-to-End Tests (`tests/e2e/`)

**Purpose**: End-to-end tests that verify user workflows from the browser perspective using Playwright.

**Framework**: Cucumber.js + Playwright (TypeScript / Node), driven from
`tests/e2e/`. The .feature files live under `tests/e2e/features` and step
definitions under `tests/e2e/step-definitions`.

**Characteristics**:
- Tests complete user scenarios
- Runs against deployed environments
- Uses real browsers (Chromium)
- Slowest tests (seconds to minutes)
- Runs after deployment in CI/CD

**Example Step Definition** (TypeScript):
```ts
When('the user logs in with valid credentials', async function () {
  await this.page.goto(`${process.env.BASE_URL}/login`);
  await this.page.fill('#email', 'user@example.com');
  await this.page.fill('#password', 'password123');
  await this.page.click("button[type='submit']");
  await this.page.waitForURL('**/dashboard');
});
```

**When to Add Tests**:
- Critical user workflows (login, checkout, etc.)
- Navigation flows
- Form submissions
- UI interactions
- Smoke tests for deployments

## Running Tests Locally

### Unit Tests
```powershell
# Run all unit tests
dotnet test tests/Api.Tests

# Run with verbose output
dotnet test tests/Api.Tests --verbosity detailed

# Run specific test
dotnet test tests/Api.Tests --filter "FullyQualifiedName~Calculator_Add"
```

### Integration Tests
```powershell
# Run all integration tests
dotnet test tests/IntegrationTests

# With test database connection string
dotnet test tests/IntegrationTests --settings test.runsettings
```

### End-to-End Tests
```powershell
# Install Playwright browsers (first time only)
cd tests/e2e
npm ci
npm run playwright:install

# Run tests against local development server
$env:BASE_URL = "http://localhost:5173"
npm test

# Run tests against deployed environment
$env:BASE_URL = "https://your-app.azurestaticapps.net"
npm test
```

## CI/CD Integration

### GitHub Actions Workflow

The tests run in the following order:

1. **Unit Tests Job**
   - Runs first, before any deployment
   - Must pass for deployment to proceed
   - Fastest feedback loop

2. **Build and Deploy Job**
   - Only runs if unit tests pass
   - Deploys to appropriate environment

3. **E2E Tests Job**
   - Runs after successful deployment
   - Tests against the deployed URL
   - Uses Playwright for browser automation

### Environment Variables

Tests can access environment variables:

```csharp
var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";
```

In CI/CD, these are set automatically:
```yaml
env:
  BASE_URL: ${{ needs.build_and_deploy_job.outputs.deployed-url }}
```

## Test Organization Best Practices

### 1. Naming Conventions
```
MethodName_StateUnderTest_ExpectedBehavior

Examples:
- Calculator_Add_WithPositiveNumbers_ReturnsSum
- UserService_CreateUser_WithInvalidEmail_ThrowsException
- LoginPage_Submit_WithValidCredentials_RedirectsToDashboard
```

### 2. Arrange-Act-Assert Pattern
```csharp
[Fact]
public void TestMethod()
{
    // Arrange - Set up test data and dependencies
    var input = "test data";
    var expected = "expected result";
    
    // Act - Execute the code under test
    var actual = MethodUnderTest(input);
    
    // Assert - Verify the results
    Assert.Equal(expected, actual);
}
```

### 3. Test Independence
- Each test should be independent
- Tests should not rely on execution order
- Clean up resources in Dispose/DisposeAsync
- Use fresh instances or reset state between tests

### 4. Test Data
- Use meaningful test data
- Avoid magic numbers/strings
- Consider test data builders for complex objects
- Use constants for commonly used values

## Common Testing Patterns

### Async Testing
```csharp
[Fact]
public async Task AsyncMethod_ReturnsExpectedResult()
{
    // Arrange
    var service = new MyService();
    
    // Act
    var result = await service.GetDataAsync();
    
    // Assert
    Assert.NotNull(result);
}
```

### Exception Testing
```csharp
[Fact]
public void Method_WithInvalidInput_ThrowsException()
{
    // Arrange
    var service = new MyService();
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => service.Process(null));
}
```

### Theory Tests (Parameterized)
```csharp
[Theory]
[InlineData(2, 3, 5)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Add_WithVariousInputs_ReturnsCorrectSum(int a, int b, int expected)
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    var result = calculator.Add(a, b);
    
    // Assert
    Assert.Equal(expected, result);
}
```

### Playwright Page Object Pattern
```csharp
public class LoginPage
{
    private readonly IPage _page;
    
    public LoginPage(IPage page) => _page = page;
    
    public async Task NavigateAsync() => 
        await _page.GotoAsync("/login");
    
    public async Task LoginAsync(string email, string password)
    {
        await _page.FillAsync("#email", email);
        await _page.FillAsync("#password", password);
        await _page.ClickAsync("button[type='submit']");
    }
}

// Usage in test
[Fact]
public async Task UserCanLogin()
{
    var page = await _browser.NewPageAsync();
    var loginPage = new LoginPage(page);
    
    await loginPage.NavigateAsync();
    await loginPage.LoginAsync("user@test.com", "password");
    
    await page.WaitForURLAsync("**/dashboard");
}
```

## Useful xUnit Attributes

- `[Fact]` - Single test method
- `[Theory]` - Parameterized test
- `[InlineData]` - Provide test data inline
- `[MemberData]` - Provide test data from method/property
- `[ClassData]` - Provide test data from class
- `[Skip]` - Temporarily skip a test
- `[Trait]` - Categorize tests (e.g., `[Trait("Category", "Integration")]`)

## Test Coverage

To generate test coverage reports:

```powershell
# Install coverlet global tool (first time only)
dotnet tool install --global coverlet.console

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate HTML report (requires ReportGenerator)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Blazor Testing Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/test)

## Contributing

When adding tests:
1. Follow the existing structure and naming conventions
2. Ensure tests are independent and repeatable
3. Add meaningful assertions
4. Document complex test scenarios
5. Run all tests before committing
6. Maintain or improve test coverage

## Current Status

⚠️ **Initial Setup**: The test projects are currently stubs with sample tests. Replace these with actual tests for your Blazor application once you have implemented features.

### Next Steps
1. Implement Blazor Client project
2. Add business logic to shared library
3. Write unit tests for business logic
4. Add integration tests for API endpoints
5. Create acceptance tests for user workflows

