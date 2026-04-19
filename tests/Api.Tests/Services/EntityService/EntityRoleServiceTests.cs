using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.Contacts;
using RajFinancial.Api.Services.EntityService;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Tests.Services.EntityService;

/// <summary>
///     Unit tests for <see cref="Api.Services.EntityService.EntityService"/> role-management operations.
/// </summary>
public class EntityRoleServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly Api.Services.EntityService.EntityService service;
    private readonly Mock<IContactResolver> contactMock;

    private static readonly Guid ownerId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");

    public EntityRoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        dbContext = new ApplicationDbContext(options);

        var authMock = new Mock<IAuthorizationService>();
        authMock.Setup(a => a.CheckAccessAsync(ownerId, ownerId, DataCategories.Entities, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner));

        var logger = new Mock<ILogger<Api.Services.EntityService.EntityService>>();
        contactMock = new Mock<IContactResolver>();
        contactMock
            .Setup(c => c.EnsureOwnedByAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        service = new Api.Services.EntityService.EntityService(dbContext, authMock.Object, contactMock.Object, logger.Object);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AssignRole_BusinessOwner_ReturnsDto()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner,
            IsSignatory = true,
            IsPrimary = true,
            SortOrder = 0,
            OwnershipPercent = 100
        };

        var result = await service.AssignRoleAsync(ownerId, entity.Id, request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.RoleType.Should().Be(EntityRoleType.Owner);
        result.OwnershipPercent.Should().Be(100);
    }

    [Fact]
    public async Task AssignRole_TrustTrustee_ReturnsDto()
    {
        var entity = await SeedEntityAsync(EntityType.Trust, "Family Trust");

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Trustee,
            IsSignatory = true,
            IsPrimary = true,
            SortOrder = 0
        };

        var result = await service.AssignRoleAsync(ownerId, entity.Id, request);

        result.RoleType.Should().Be(EntityRoleType.Trustee);
    }

    [Fact]
    public async Task AssignRole_TrustRoleOnBusiness_ThrowsBusinessRuleException()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Trustee,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0
        };

        var act = () => service.AssignRoleAsync(ownerId, entity.Id, request);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.ROLE_INVALID_FOR_ENTITY_TYPE);
    }

    [Fact]
    public async Task AssignRole_BusinessRoleOnTrust_ThrowsBusinessRuleException()
    {
        var entity = await SeedEntityAsync(EntityType.Trust, "Family Trust");

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Officer,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0
        };

        var act = () => service.AssignRoleAsync(ownerId, entity.Id, request);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.ROLE_INVALID_FOR_ENTITY_TYPE);
    }

    [Fact]
    public async Task AssignRole_OwnershipExceeds100_ThrowsBusinessRuleException()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        dbContext.EntityRoles.Add(new EntityRole
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner,
            OwnershipPercent = 75m
        });
        await dbContext.SaveChangesAsync();

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner,
            OwnershipPercent = 30,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 1
        };

        var act = () => service.AssignRoleAsync(ownerId, entity.Id, request);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.ROLE_OWNERSHIP_EXCEEDS_100);
    }

    [Fact]
    public async Task AssignRole_BeneficialInterest_ReturnsDto()
    {
        var entity = await SeedEntityAsync(EntityType.Trust, "Family Trust");

        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Beneficiary,
            BeneficialInterestPercent = 50,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0
        };

        var result = await service.AssignRoleAsync(ownerId, entity.Id, request);

        result.RoleType.Should().Be(EntityRoleType.Beneficiary);
        result.BeneficialInterestPercent.Should().Be(50);
    }

    [Fact]
    public async Task GetRoles_ReturnsAllRolesForEntity()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        dbContext.EntityRoles.AddRange(
            new EntityRole
            {
                Id = Guid.NewGuid(), EntityId = entity.Id,
                ContactId = Guid.NewGuid(), RoleType = EntityRoleType.Owner
            },
            new EntityRole
            {
                Id = Guid.NewGuid(), EntityId = entity.Id,
                ContactId = Guid.NewGuid(), RoleType = EntityRoleType.Officer
            });
        await dbContext.SaveChangesAsync();

        var results = await service.GetRolesAsync(ownerId, entity.Id);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveRole_Exists_Succeeds()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        var role = new EntityRole
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner
        };
        dbContext.EntityRoles.Add(role);
        await dbContext.SaveChangesAsync();

        await service.RemoveRoleAsync(ownerId, entity.Id, role.Id);

        var inDb = await dbContext.EntityRoles.FindAsync(role.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task RemoveRole_NotFound_ThrowsNotFoundException()
    {
        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");

        var act = () => service.RemoveRoleAsync(ownerId, entity.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // =========================================================================
    // IContactResolver integration (Phase 1 cross-tenant guard)
    // =========================================================================

    [Fact]
    public async Task AssignRole_ResolverRejectsContact_ThrowsRoleContactNotFound()
    {
        // Re-stub the resolver to reject every id (prod-default behavior)
        contactMock.Reset();
        contactMock
            .Setup(c => c.EnsureOwnedByAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(
                EntityErrorCodes.ROLE_CONTACT_NOT_FOUND, "Contact not found."));

        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0,
            OwnershipPercent = 50
        };

        var act = () => service.AssignRoleAsync(ownerId, entity.Id, request);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.ROLE_CONTACT_NOT_FOUND);

        // Role must not have been persisted
        dbContext.EntityRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignRole_ResolverCalledWithSuppliedContactIdAndEntityOwner()
    {
        // Verify the service passes the caller-supplied ContactId *and* the
        // entity's owner userId (not the request's userId, not a hardcoded one).
        Guid? capturedContactId = null;
        Guid? capturedUserId = null;
        contactMock.Reset();
        contactMock
            .Setup(c => c.EnsureOwnedByAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((cid, uid, _) =>
            {
                capturedContactId = cid;
                capturedUserId = uid;
            })
            .Returns(Task.CompletedTask);

        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        var suppliedContactId = Guid.NewGuid();
        var request = new CreateEntityRoleRequest
        {
            ContactId = suppliedContactId,
            RoleType = EntityRoleType.Owner,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0,
            OwnershipPercent = 50
        };

        await service.AssignRoleAsync(ownerId, entity.Id, request);

        capturedContactId.Should().Be(suppliedContactId);
        capturedUserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task AssignRole_ResolverNotConsultedWhenAuthorizationDenies()
    {
        // Ordering guard: if auth fails, the resolver must not be called —
        // otherwise a caller could probe contact existence without being
        // authorized for the target entity.
        var strangerAuthMock = new Mock<IAuthorizationService>();
        strangerAuthMock
            .Setup(a => a.CheckAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                DataCategories.Entities, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Deny());

        // Strict mock with no setup — any invocation fails the test.
        var strictContactMock = new Mock<IContactResolver>(MockBehavior.Strict);

        var strangerService = new Api.Services.EntityService.EntityService(
            dbContext,
            strangerAuthMock.Object,
            strictContactMock.Object,
            Mock.Of<ILogger<Api.Services.EntityService.EntityService>>());

        var entity = await SeedEntityAsync(EntityType.Business, "Acme LLC");
        var stranger = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
        var request = new CreateEntityRoleRequest
        {
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Owner,
            IsSignatory = false,
            IsPrimary = false,
            SortOrder = 0,
            OwnershipPercent = 50
        };

        var act = () => strangerService.AssignRoleAsync(stranger, entity.Id, request);

        // Auth denial surfaces as NotFound (IDOR defense). Resolver never called.
        await act.Should().ThrowAsync<NotFoundException>();
        strictContactMock.VerifyNoOtherCalls();
    }

    private async Task<Entity> SeedEntityAsync(EntityType type, string name)
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Type = type,
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Entities.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }
}
