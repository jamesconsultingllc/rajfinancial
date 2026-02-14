using System.Security.Claims;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Fluent builder for creating ClaimsPrincipal instances for integration tests.
/// </summary>
internal class TestClaimsBuilder
{
    private readonly List<Claim> claims = [];
    private string authenticationType = "Bearer";

    public TestClaimsBuilder WithUserId(string objectId)
    {
        claims.Add(new Claim("oid", objectId));
        return this;
    }

    public TestClaimsBuilder WithEmail(string email)
    {
        claims.Add(new Claim("emails", email));
        return this;
    }

    public TestClaimsBuilder WithName(string name)
    {
        claims.Add(new Claim("name", name));
        return this;
    }

    public TestClaimsBuilder WithRole(string role)
    {
        claims.Add(new Claim("roles", role));
        return this;
    }

    public TestClaimsBuilder WithRoles(params string[] roles)
    {
        foreach (var role in roles)
            claims.Add(new Claim("roles", role));
        return this;
    }

    public TestClaimsBuilder Unauthenticated()
    {
        authenticationType = null!;
        return this;
    }

    public ClaimsPrincipal Build()
    {
        var identity = new ClaimsIdentity(claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates an authenticated user with standard claims.
    /// </summary>
    public static ClaimsPrincipal AuthenticatedUser(
        string email = "user@example.com",
        string? userId = null,
        params string[] roles)
    {
        var builder = new TestClaimsBuilder()
            .WithUserId(userId ?? Guid.NewGuid().ToString())
            .WithEmail(email)
            .WithName(email.Split('@')[0]);

        foreach (var role in roles)
            builder.WithRole(role);

        return builder.Build();
    }

    /// <summary>
    /// Creates an administrator principal.
    /// </summary>
    public static ClaimsPrincipal Administrator(
        string email = "admin@example.com",
        string? userId = null)
    {
        return AuthenticatedUser(email, userId, "Administrator");
    }
}
