using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Fluent builder for creating <see cref="ClaimsPrincipal"/> instances and JWT Bearer tokens
/// for integration tests. JWTs are unsigned — the <c>AuthenticationMiddleware</c> in Development
/// mode parses them without signature validation, enabling full middleware pipeline testing.
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
            .WithUserId(userId ?? DeterministicUserId(email))
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

    /// <summary>
    /// Builds an unsigned JWT Bearer token string from the current claims.
    /// Suitable for integration tests against a Development-mode Functions host
    /// where <c>AuthenticationMiddleware</c> parses JWTs without signature validation.
    /// </summary>
    public string BuildJwtToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var identity = new ClaimsIdentity(claims, authenticationType);
        return handler.CreateEncodedJwt(
            issuer: "https://test-issuer.example.com",
            audience: "test-audience",
            subject: identity,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);
    }

    /// <summary>
    /// Creates an unsigned JWT token for an authenticated user with the given roles.
    /// </summary>
    public static string JwtForUser(
        string email = "user@example.com",
        string? userId = null,
        params string[] roles)
    {
        var builder = new TestClaimsBuilder()
            .WithUserId(userId ?? DeterministicUserId(email))
            .WithEmail(email)
            .WithName(email.Split('@')[0]);

        foreach (var role in roles)
            builder.WithRole(role);

        return builder.BuildJwtToken();
    }

    /// <summary>
    /// Creates an unsigned JWT token for an administrator.
    /// </summary>
    public static string JwtForAdmin(
        string email = "admin@example.com",
        string? userId = null)
    {
        return JwtForUser(email, userId, "Administrator");
    }

    /// <summary>
    /// Generates a deterministic GUID from an email address using MD5 hashing.
    /// Ensures that the same email always produces the same <c>oid</c> claim across
    /// test runs, which prevents <c>UserProfile</c> unique-constraint violations on
    /// <c>(TenantId, Email)</c> when the <c>UserProfileProvisioningMiddleware</c>
    /// JIT-provisions profiles against a real database.
    /// </summary>
    internal static string DeterministicUserId(string email)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return new Guid(hash).ToString();
    }
}
