using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Services.UserProfile;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Tests.Services.UserProfile;

/// <summary>
/// TDD unit tests for <see cref="UserProfileService"/>.
/// Tests JIT provisioning logic: create on first access, sync mutable claims,
/// role mapping with priority, and LastLoginAt stamping.
/// </summary>
public sealed class UserProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<ILogger<UserProfileService>> loggerMock;

    // Fixed test GUIDs
    private static readonly Guid UserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
    private static readonly Guid UnknownUserId = Guid.Parse("cccc0000-0000-0000-0000-000000000003");

    public UserProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbContext = new ApplicationDbContext(options);
        loggerMock = new Mock<ILogger<UserProfileService>>();
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }

    private UserProfileService CreateService() => new(dbContext, loggerMock.Object);

    // =========================================================================
    // EnsureProfileExistsAsync — New user (JIT provisioning)
    // =========================================================================

    [Fact]
    public async Task EnsureProfileExists_NewUser_CreatesProfileWithCorrectId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.Id.Should().Be(UserId);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsEmail()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.Email.Should().Be("user@rajfinancial.com");
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsDisplayName()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_DefaultsToClientRole()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsIsActiveToTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsCreatedAtToRecent()
    {
        // Arrange
        var service = CreateService();
        var before = DateTime.UtcNow;

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var after = DateTime.UtcNow;
        profile.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsLastLoginAtToRecent()
    {
        // Arrange
        var service = CreateService();
        var before = DateTime.UtcNow;

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var after = DateTime.UtcNow;
        profile.LastLoginAt.Should().NotBeNull();
        profile.LastLoginAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_PersistsToDatabase()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var persisted = await dbContext.UserProfiles.FindAsync(UserId);
        persisted.Should().NotBeNull();
        persisted.Email.Should().Be("user@rajfinancial.com");
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_WithTenantId_SetsTenantId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"], TenantId);

        // Assert
        profile.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_WithNullDisplayName_SetsEmptyDisplayName()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", null, ["Client"]);

        // Assert
        profile.DisplayName.Should().BeEmpty();
    }

    // =========================================================================
    // EnsureProfileExistsAsync — Role mapping with priority
    // =========================================================================

    [Fact]
    public async Task EnsureProfileExists_WithAdministratorRole_MapsToAdministrator()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "admin@rajfinancial.com", "Admin User", ["Administrator"]);

        // Assert
        profile.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task EnsureProfileExists_WithAdvisorRole_MapsToAdvisor()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "advisor@rajfinancial.com", "Advisor User", ["Advisor"]);

        // Assert
        profile.Role.Should().Be(UserRole.Advisor);
    }

    [Fact]
    public async Task EnsureProfileExists_WithMultipleRoles_MapsHighestPriority_AdminOverAdvisor()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "admin@rajfinancial.com", "Multi-Role User",
            ["Client", "Advisor", "Administrator"]);

        // Assert
        profile.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task EnsureProfileExists_WithMultipleRoles_MapsHighestPriority_AdvisorOverClient()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "advisor@rajfinancial.com", "Multi-Role User",
            ["Client", "Advisor"]);

        // Assert
        profile.Role.Should().Be(UserRole.Advisor);
    }

    [Fact]
    public async Task EnsureProfileExists_WithEmptyRoles_DefaultsToClient()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "No Role User", []);

        // Assert
        profile.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task EnsureProfileExists_WithUnknownRoleOnly_DefaultsToClient()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "Unknown Role User", ["SomeUnknownRole"]);

        // Assert
        profile.Role.Should().Be(UserRole.Client);
    }

    // =========================================================================
    // EnsureProfileExistsAsync — Returning user (sync mutable claims)
    // =========================================================================

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_UpdatesLastLoginAt()
    {
        // Arrange
        var service = CreateService();
        var initialProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Set LastLoginAt to a known past time
        initialProfile.LastLoginAt = DateTime.UtcNow.AddMinutes(-10);
        await dbContext.SaveChangesAsync();

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.LastLoginAt.Should().BeCloseTo(
            DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_AlwaysUpdatesLastLoginAt()
    {
        // Arrange
        var service = CreateService();
        var initialProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Set LastLoginAt to a known past time so the update is detectable
        initialProfile.LastLoginAt = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();
        var previousLastLogin = initialProfile.LastLoginAt;

        // Act — called again; LastLoginAt should always be refreshed
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert — LastLoginAt should have been updated to now
        updatedProfile.LastLoginAt.Should().BeAfter(previousLastLogin!.Value);
        updatedProfile.LastLoginAt.Should().BeCloseTo(
            DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_DoesNotChangeCreatedAt()
    {
        // Arrange
        var service = CreateService();
        var initialProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);
        var originalCreatedAt = initialProfile.CreatedAt;

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_DoesNotCreateDuplicateProfile()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var count = await dbContext.UserProfiles.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SyncsChangedEmail()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "old@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "new@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.Email.Should().Be("new@rajfinancial.com");
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SyncsChangedDisplayName()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "Old Name", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "New Name", ["Client"]);

        // Assert
        updatedProfile.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SyncsChangedRole()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Administrator"]);

        // Assert
        updatedProfile.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SetsUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "Old Name", ["Client"]);
        var before = DateTime.UtcNow;

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "New Name", ["Client"]);

        // Assert
        var after = DateTime.UtcNow;
        updatedProfile.UpdatedAt.Should().NotBeNull();
        updatedProfile.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // =========================================================================
    // GetByIdAsync
    // =========================================================================

    [Fact]
    public async Task GetById_ExistingUser_ReturnsProfile()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var profile = await service.GetByIdAsync(UserId);

        // Assert
        profile.Should().NotBeNull();
        profile.Id.Should().Be(UserId);
        profile.Email.Should().Be("user@rajfinancial.com");
        profile.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetById_UnknownUser_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.GetByIdAsync(UnknownUserId);

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ReturnsAllPersistedFields()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            UserId, "admin@rajfinancial.com", "Admin User",
            ["Administrator"], TenantId);

        // Act
        var profile = await service.GetByIdAsync(UserId);

        // Assert
        profile.Should().NotBeNull();
        profile.Id.Should().Be(UserId);
        profile.Email.Should().Be("admin@rajfinancial.com");
        profile.DisplayName.Should().Be("Admin User");
        profile.Role.Should().Be(UserRole.Administrator);
        profile.TenantId.Should().Be(TenantId);
        profile.IsActive.Should().BeTrue();
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        profile.LastLoginAt.Should().NotBeNull();
    }

    // =========================================================================
    // UpdateProfileAsync
    // =========================================================================

    [Fact]
    public async Task UpdateProfile_ExistingUser_UpdatesDisplayName()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(UserId, "user@rajfinancial.com", "Old Name", ["Client"]);
        var request = new UpdateProfileRequest
        {
            DisplayName = "New Name",
            Locale = "es-MX",
            Timezone = "America/Chicago",
            Currency = "EUR"
        };

        // Act
        var result = await service.UpdateProfileAsync(UserId, request);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProfile_ExistingUser_SetsPreferencesJson()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(UserId, "user@rajfinancial.com", "User", ["Client"]);
        var request = new UpdateProfileRequest
        {
            DisplayName = "User",
            Locale = "es-MX",
            Timezone = "America/Chicago",
            Currency = "EUR"
        };

        // Act
        var result = await service.UpdateProfileAsync(UserId, request);

        // Assert
        result.Should().NotBeNull();
        result.PreferencesJson.Should().Contain("es-MX");
        result.PreferencesJson.Should().Contain("America/Chicago");
        result.PreferencesJson.Should().Contain("EUR");
    }

    [Fact]
    public async Task UpdateProfile_ExistingUser_StampsUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(UserId, "user@rajfinancial.com", "User", ["Client"]);
        var request = new UpdateProfileRequest
        {
            DisplayName = "User",
            Locale = "en-US",
            Timezone = "America/New_York",
            Currency = "USD"
        };

        // Act
        var result = await service.UpdateProfileAsync(UserId, request);

        // Assert
        result.Should().NotBeNull();
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateProfile_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var request = new UpdateProfileRequest
        {
            DisplayName = "Ghost",
            Locale = "en-US",
            Timezone = "America/New_York",
            Currency = "USD"
        };

        // Act
        var result = await service.UpdateProfileAsync(UnknownUserId, request);

        // Assert
        result.Should().BeNull();
    }
}
