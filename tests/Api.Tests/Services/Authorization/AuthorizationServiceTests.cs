using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Tests.Services.Authorization;

/// <summary>
/// Unit tests for <see cref="AuthorizationService"/>.
/// Tests the three-tier resource-level authorization check:
/// 1. Resource Owner, 2. DataAccessGrant, 3. Administrator.
/// Covers OWASP A01:2025 (Broken Access Control / IDOR prevention).
/// </summary>
public class AuthorizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthorizationService _service;

    // Fixed test GUIDs
    private static readonly Guid OwnerId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid GranteeId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
    private static readonly Guid AdminId = Guid.Parse("cccc0000-0000-0000-0000-000000000003");
    private static readonly Guid StrangerId = Guid.Parse("dddd0000-0000-0000-0000-000000000004");

    public AuthorizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AuthorizationService>>();
        _service = new AuthorizationService(_dbContext, logger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // =========================================================================
    // Tier 1: Resource Owner
    // =========================================================================

    [Fact]
    public async Task CheckAccess_OwnerAccessesOwnResource_GrantsWithOwnerReason()
    {
        var decision = await _service.CheckAccessAsync(
            OwnerId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
        decision.GrantedAccessLevel.Should().Be(AccessType.Owner);
    }

    [Fact]
    public async Task CheckAccess_OwnerAccessesOwnResource_GrantsRegardlessOfCategory()
    {
        var decision = await _service.CheckAccessAsync(
            OwnerId, OwnerId, DataCategories.Documents, AccessType.Full);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
    }

    [Fact]
    public async Task CheckAccess_OwnerAccessesOwnResource_GrantsRegardlessOfRequiredLevel()
    {
        var decision = await _service.CheckAccessAsync(
            OwnerId, OwnerId, DataCategories.Accounts, AccessType.Owner);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
        decision.GrantedAccessLevel.Should().Be(AccessType.Owner);
    }

    // =========================================================================
    // Tier 2: DataAccessGrant — active grant with matching category/level
    // =========================================================================

    [Fact]
    public async Task CheckAccess_GranteeWithActiveReadGrant_GrantsWithDataAccessGrantReason()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
        decision.GrantedAccessLevel.Should().Be(AccessType.Read);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithActiveFullGrant_GrantsForReadRequest()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Full, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
        decision.GrantedAccessLevel.Should().Be(AccessType.Full);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithActiveFullGrant_GrantsForFullRequest()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Full, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Full);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
        decision.GrantedAccessLevel.Should().Be(AccessType.Full);
    }

    // =========================================================================
    // Tier 2: DataAccessGrant — denied scenarios
    // =========================================================================

    [Fact]
    public async Task CheckAccess_GranteeWithExpiredGrant_Denies()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Expired,
            categories: [DataCategories.Accounts]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithRevokedGrant_Denies()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Revoked,
            categories: [DataCategories.Accounts]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithPendingGrant_Denies()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Pending,
            categories: [DataCategories.Accounts]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithLimitedAccessToWrongCategory_Denies()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Limited, GrantStatus.Active,
            categories: [DataCategories.Documents]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithReadGrant_DeniesFullAccessRequest()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Full);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithExpiresAtInPast_Denies()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts],
            expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithExpiresAtInFuture_Grants()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts],
            expiresAt: DateTimeOffset.UtcNow.AddDays(30));

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
    }

    [Fact]
    public async Task CheckAccess_GranteeWithNullExpiresAt_Grants()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts],
            expiresAt: null);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
    }

    // =========================================================================
    // Tier 2: DataAccessGrant — Full/Read grants cover all categories
    // =========================================================================

    [Fact]
    public async Task CheckAccess_FullGrantCoversAnyCategory()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Full, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Documents, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
    }

    [Fact]
    public async Task CheckAccess_ReadGrantCoversAnyCategory()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Assets, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
    }

    [Fact]
    public async Task CheckAccess_LimitedGrantWithMatchingCategory_Grants()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Limited, GrantStatus.Active,
            categories: [DataCategories.Accounts, DataCategories.Assets]);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
        decision.GrantedAccessLevel.Should().Be(AccessType.Limited);
    }

    // =========================================================================
    // Tier 3: Administrator
    // =========================================================================

    [Fact]
    public async Task CheckAccess_Administrator_GrantsWithAdminReason()
    {
        SeedUserProfile(AdminId, UserRole.Administrator);

        var decision = await _service.CheckAccessAsync(
            AdminId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.Administrator);
        decision.GrantedAccessLevel.Should().Be(AccessType.Full);
    }

    [Fact]
    public async Task CheckAccess_Administrator_GrantsForAnyCategory()
    {
        SeedUserProfile(AdminId, UserRole.Administrator);

        var decision = await _service.CheckAccessAsync(
            AdminId, OwnerId, DataCategories.Documents, AccessType.Full);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.Administrator);
    }

    // =========================================================================
    // Denied: No matching tier (IDOR prevention)
    // =========================================================================

    [Fact]
    public async Task CheckAccess_StrangerWithNoGrant_Denies()
    {
        var decision = await _service.CheckAccessAsync(
            StrangerId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    [Fact]
    public async Task CheckAccess_NonAdminUserWithNoGrant_Denies()
    {
        SeedUserProfile(StrangerId, UserRole.Client);

        var decision = await _service.CheckAccessAsync(
            StrangerId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    // =========================================================================
    // Tier priority: Owner beats DataAccessGrant beats Administrator
    // =========================================================================

    [Fact]
    public async Task CheckAccess_OwnerWhoIsAlsoAdmin_ReturnsOwnerReason()
    {
        SeedUserProfile(OwnerId, UserRole.Administrator);

        var decision = await _service.CheckAccessAsync(
            OwnerId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
    }

    [Fact]
    public async Task CheckAccess_GranteeWhoIsAlsoAdmin_ReturnsDataAccessGrantReason()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Read, GrantStatus.Active,
            categories: [DataCategories.Accounts]);
        SeedUserProfile(GranteeId, UserRole.Administrator);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public async Task CheckAccess_CategoryAll_FullGrantCovers()
    {
        SeedGrant(OwnerId, GranteeId, AccessType.Full, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.All, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAccess_GrantForDifferentOwner_Denies()
    {
        var otherOwnerId = Guid.Parse("eeee0000-0000-0000-0000-000000000005");
        SeedGrant(otherOwnerId, GranteeId, AccessType.Full, GrantStatus.Active);

        var decision = await _service.CheckAccessAsync(
            GranteeId, OwnerId, DataCategories.Accounts, AccessType.Read);

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void SeedGrant(
        Guid grantorId,
        Guid granteeId,
        AccessType accessType,
        GrantStatus status,
        List<string>? categories = null,
        DateTimeOffset? expiresAt = null)
    {
        _dbContext.DataAccessGrants.Add(new DataAccessGrant
        {
            Id = Guid.NewGuid(),
            GrantorUserId = grantorId,
            GranteeUserId = granteeId,
            GranteeEmail = "grantee@example.com",
            AccessType = accessType,
            Status = status,
            Categories = categories ?? [],
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
            ExpiresAt = expiresAt
        });
        _dbContext.SaveChanges();
    }

    private void SeedUserProfile(Guid userId, UserRole role)
    {
        // Only add if not already seeded
        if (!_dbContext.UserProfiles.Any(u => u.Id == userId))
        {
            _dbContext.UserProfiles.Add(new UserProfile
            {
                Id = userId,
                Email = $"{role.ToString().ToLowerInvariant()}@example.com",
                DisplayName = role.ToString(),
                FirstName = role.ToString(),
                LastName = "User",
                Role = role,
                TenantId = Guid.NewGuid(),
                IsActive = true
            });
            _dbContext.SaveChanges();
        }
    }
}
