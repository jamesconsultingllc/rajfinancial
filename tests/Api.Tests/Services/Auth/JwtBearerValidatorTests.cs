using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Services.Auth;

namespace RajFinancial.Api.Tests.Services.Auth;

/// <summary>
///     Unit tests for <see cref="JwtBearerValidator"/> — signature, issuer, audience,
///     lifetime validation and graceful handling of discovery outages.
/// </summary>
public class JwtBearerValidatorTests : IDisposable
{
    private const string Issuer = "https://496527a2-41f8-4297-a979-c916e7255a22.ciamlogin.com/496527a2-41f8-4297-a979-c916e7255a22/v2.0";
    private const string Audience = "api://rajfinancial-api-dev";

    private readonly RSA rsa;
    private readonly RsaSecurityKey signingKey;
    private readonly SigningCredentials signingCredentials;
    private readonly EntraExternalIdOptions options;
    private readonly Mock<IConfigurationManager<OpenIdConnectConfiguration>> configManagerMock;
    private readonly JwtBearerValidator validator;
    private bool disposed;

    public JwtBearerValidatorTests()
    {
        rsa = RSA.Create(2048);
        signingKey = new RsaSecurityKey(rsa) { KeyId = "test-key-1" };
        signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        options = new EntraExternalIdOptions
        {
            Instance = "https://rajfinancialdev.ciamlogin.com/",
            TenantId = "496527a2-41f8-4297-a979-c916e7255a22",
            ClientId = "211438af-9f00-47be-a367-796dd7770113",
            ValidAudiences = new List<string> { Audience, "211438af-9f00-47be-a367-796dd7770113" },
        };

        var config = new OpenIdConnectConfiguration { Issuer = Issuer };
        config.SigningKeys.Add(signingKey);

        configManagerMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        configManagerMock.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        validator = new JwtBearerValidator(
            configManagerMock.Object,
            Options.Create(options),
            NullLogger<JwtBearerValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsAuthenticatedPrincipal()
    {
        var token = CreateToken();

        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
        principal.FindFirst("oid")!.Value.Should().Be("aaaa0000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task ValidateAsync_PreservesOriginalClaimTypes_WithMapInboundClaimsFalse()
    {
        // Arrange — token carries short names "oid", "emails", "roles", "tid"
        var token = CreateToken();

        // Act
        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        // Assert — MapInboundClaims=false means claim types stay as emitted
        principal!.FindFirst("oid").Should().NotBeNull();
        principal.FindFirst("tid").Should().NotBeNull();
        principal.FindFirst("emails").Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WrongSignature_ReturnsNull()
    {
        using var otherRsa = RSA.Create(2048);
        var otherKey = new RsaSecurityKey(otherRsa) { KeyId = "other-key" };
        var token = CreateToken(signingCredentials: new SigningCredentials(otherKey, SecurityAlgorithms.RsaSha256));

        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WrongAudience_ReturnsNull()
    {
        var token = CreateToken(audience: "api://some-other-app");

        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WrongIssuer_ReturnsNull()
    {
        var token = CreateToken(issuer: "https://contoso.ciamlogin.com/other/v2.0");

        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnsNull()
    {
        var token = CreateToken(
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1));

        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_MalformedToken_ReturnsNull()
    {
        var principal = await validator.ValidateAsync("not.a.valid.jwt.token", CancellationToken.None);

        principal.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyOrWhitespaceToken_ReturnsNull(string token)
    {
        var principal = await validator.ValidateAsync(token, CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_DiscoveryOutage_ReturnsNullInsteadOfThrowing()
    {
        var outageMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        outageMock.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated network failure"));

        var outageValidator = new JwtBearerValidator(
            outageMock.Object,
            Options.Create(options),
            NullLogger<JwtBearerValidator>.Instance);

        var principal = await outageValidator.ValidateAsync(CreateToken(), CancellationToken.None);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_UsesIssuerFromDiscovery_NotTemplatedValue()
    {
        // If the issuer in the discovery doc ever changes, the validator must honour it.
        var newIssuer = "https://updated-issuer.example/v2.0";
        var newConfig = new OpenIdConnectConfiguration { Issuer = newIssuer };
        newConfig.SigningKeys.Add(signingKey);

        var freshMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        freshMock.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);

        var freshValidator = new JwtBearerValidator(
            freshMock.Object,
            Options.Create(options),
            NullLogger<JwtBearerValidator>.Instance);

        var token = CreateToken(issuer: newIssuer);

        var principal = await freshValidator.ValidateAsync(token, CancellationToken.None);

        principal.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_KeyRotation_NewKeyFromDiscoveryIsAccepted()
    {
        // First pass: old config + old key issues a token, config then rotates.
        using var rotatedRsa = RSA.Create(2048);
        var rotatedKey = new RsaSecurityKey(rotatedRsa) { KeyId = "rotated-key" };
        var rotatedCreds = new SigningCredentials(rotatedKey, SecurityAlgorithms.RsaSha256);

        var rotatedConfig = new OpenIdConnectConfiguration { Issuer = Issuer };
        rotatedConfig.SigningKeys.Add(rotatedKey);

        var rotatedMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        rotatedMock.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rotatedConfig);

        var rotatedValidator = new JwtBearerValidator(
            rotatedMock.Object,
            Options.Create(options),
            NullLogger<JwtBearerValidator>.Instance);

        var tokenSignedWithRotatedKey = CreateToken(signingCredentials: rotatedCreds);

        var principal = await rotatedValidator.ValidateAsync(tokenSignedWithRotatedKey, CancellationToken.None);

        principal.Should().NotBeNull();
    }

    // ------------------------------------------------------------------
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            rsa.Dispose();
        }

        disposed = true;
    }

    private string CreateToken(
        string? issuer = null,
        string? audience = null,
        DateTime? notBefore = null,
        DateTime? expires = null,
        SigningCredentials? signingCredentials = null)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var identity = new ClaimsIdentity(
        [
            new Claim("oid", "aaaa0000-0000-0000-0000-000000000001"),
            new Claim("tid", "496527a2-41f8-4297-a979-c916e7255a22"),
            new Claim("emails", "user@rajfinancial.com"),
            new Claim("roles", "Client"),
            new Claim("name", "Test User"),
        ]);

        return handler.CreateEncodedJwt(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            subject: identity,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: signingCredentials ?? this.signingCredentials);
    }
}
