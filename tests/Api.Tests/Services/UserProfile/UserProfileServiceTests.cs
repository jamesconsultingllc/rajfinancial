using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Tests.Services.UserProfiles;

/// <summary>
/// TDD unit tests for <see cref="UserProfileService"/>.
/// Tests JIT provisioning logic: create on first access, sync mutable claims,
/// role mapping with priority, and LastLoginAt stamping.
/// </summary>
public class UserProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<ILogger<UserProfileService>> loggerMock;

    // Fixed test GUIDs
    private static readonly Guid userId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid tenantId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
    private static readonly Guid unknownUserId = Guid.Parse("cccc0000-0000-0000-0000-000000000003");

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
        GC.SuppressFinalize(this);
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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.Id.Should().Be(userId);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsEmail()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsCreatedAtToRecent()
    {
        // Arrange
        var service = CreateService();
        var before = DateTimeOffset.UtcNow;

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var after = DateTimeOffset.UtcNow;
        profile.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_SetsLastLoginAtToRecent()
    {
        // Arrange
        var service = CreateService();
        var before = DateTimeOffset.UtcNow;

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var after = DateTimeOffset.UtcNow;
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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        var persisted = await dbContext.UserProfiles.FindAsync(userId);
        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be("user@rajfinancial.com");
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_WithTenantId_SetsTenantId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"], tenantId);

        // Assert
        profile.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task EnsureProfileExists_NewUser_WithNullDisplayName_SetsEmptyDisplayName()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", null, ["Client"]);

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
            userId, "admin@rajfinancial.com", "Admin User", ["Administrator"]);

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
            userId, "advisor@rajfinancial.com", "Advisor User", ["Advisor"]);

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
            userId, "admin@rajfinancial.com", "Multi-Role User",
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
            userId, "advisor@rajfinancial.com", "Multi-Role User",
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
            userId, "user@rajfinancial.com", "No Role User", []);

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
            userId, "user@rajfinancial.com", "Unknown Role User", ["SomeUnknownRole"]);

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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);
        var originalLastLogin = initialProfile.LastLoginAt;

        // Small delay to ensure timestamp difference
        await Task.Delay(10);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.LastLoginAt.Should().BeAfter(originalLastLogin!.Value);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_DoesNotChangeCreatedAt()
    {
        // Arrange
        var service = CreateService();
        var initialProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);
        var originalCreatedAt = initialProfile.CreatedAt;

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_DoesNotCreateDuplicateProfile()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

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
            userId, "old@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "new@rajfinancial.com", "John Doe", ["Client"]);

        // Assert
        updatedProfile.Email.Should().Be("new@rajfinancial.com");
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SyncsChangedDisplayName()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "Old Name", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "New Name", ["Client"]);

        // Assert
        updatedProfile.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SyncsChangedRole()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "John Doe", ["Administrator"]);

        // Assert
        updatedProfile.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task EnsureProfileExists_ReturningUser_SetsUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "Old Name", ["Client"]);
        var before = DateTimeOffset.UtcNow;

        // Act
        var updatedProfile = await service.EnsureProfileExistsAsync(
            userId, "user@rajfinancial.com", "New Name", ["Client"]);

        // Assert
        var after = DateTimeOffset.UtcNow;
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
            userId, "user@rajfinancial.com", "John Doe", ["Client"]);

        // Act
        var profile = await service.GetByIdAsync(userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(userId);
        profile.Email.Should().Be("user@rajfinancial.com");
        profile.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetById_UnknownUser_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var profile = await service.GetByIdAsync(unknownUserId);

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ReturnsAllPersistedFields()
    {
        // Arrange
        var service = CreateService();
        await service.EnsureProfileExistsAsync(
            userId, "admin@rajfinancial.com", "Admin User",
            ["Administrator"], tenantId);

        // Act
        var profile = await service.GetByIdAsync(userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(userId);
        profile.Email.Should().Be("admin@rajfinancial.com");
        profile.DisplayName.Should().Be("Admin User");
        profile.Role.Should().Be(UserRole.Administrator);
        profile.TenantId.Should().Be(tenantId);
        profile.IsActive.Should().BeTrue();
        profile.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        profile.LastLoginAt.Should().NotBeNull();
    }
}
