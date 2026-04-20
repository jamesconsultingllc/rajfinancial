using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.Contacts;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Contracts.Entities.Business;
using RajFinancial.Shared.Contracts.Entities.Trust;

namespace RajFinancial.Api.Tests.Services.EntityService;

/// <summary>
///     Unit tests for <see cref="Api.Services.EntityService.EntityService"/>.
///     Uses InMemoryDatabase for EF Core and mocked IAuthorizationService.
/// </summary>
public class EntityServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<IAuthorizationService> authMock;
    private readonly Api.Services.EntityService.EntityService service;

    private static readonly Guid OwnerId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid StrangerId = Guid.Parse("dddd0000-0000-0000-0000-000000000004");

    public EntityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        dbContext = new ApplicationDbContext(options);
        authMock = new Mock<IAuthorizationService>();
        var logger = new Mock<ILogger<Api.Services.EntityService.EntityService>>();

        authMock.Setup(a => a.CheckAccessAsync(OwnerId, OwnerId, DataCategories.Entities, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner));

        authMock.Setup(a => a.CheckAccessAsync(StrangerId, OwnerId, DataCategories.Entities, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Deny());

        var contactMock = new Mock<IContactResolver>();
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

    // =========================================================================
    // CreateEntityAsync
    // =========================================================================

    [Fact]
    public async Task CreateEntity_Business_ReturnsDto()
    {
        var request = new CreateEntityRequest
        {
            Name = "Acme LLC",
            Type = EntityType.Business,
            Business = new BusinessEntityMetadata
            {
                EntityFormationType = BusinessFormationType.SingleMemberLlc,
                Ein = "12-3456789"
            }
        };

        var result = await service.CreateEntityAsync(OwnerId, request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Type.Should().Be(EntityType.Business);
        result.Name.Should().Be("Acme LLC");
        result.Business.Should().NotBeNull();
        result.Business!.Ein.Should().Be("12-3456789");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateEntity_Trust_ReturnsDto()
    {
        var request = new CreateEntityRequest
        {
            Name = "Smith Family Trust",
            Type = EntityType.Trust,
            Trust = new TrustEntityMetadata
            {
                Category = TrustCategory.Irrevocable,
                CreationMethod = TrustCreationMethod.InterVivos,
                Type = TrustType.Family,
                TaxStatus = TrustTaxStatus.NonGrantor
            }
        };

        var result = await service.CreateEntityAsync(OwnerId, request);

        result.Type.Should().Be(EntityType.Trust);
        result.Trust.Should().NotBeNull();
        result.Trust!.Category.Should().Be(TrustCategory.Irrevocable);
    }

    [Fact]
    public async Task CreateEntity_PersonalAlreadyExists_ThrowsBusinessRuleException()
    {
        await SeedEntityAsync(OwnerId, "Personal", EntityType.Personal);

        var request = new CreateEntityRequest
        {
            Name = "Another Personal",
            Type = EntityType.Personal
        };

        var act = () => service.CreateEntityAsync(OwnerId, request);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.PersonalAlreadyExists);
    }

    [Fact]
    public async Task CreateEntity_DuplicateSlug_ThrowsConflictException()
    {
        await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business, slug: "acme-llc");

        var request = new CreateEntityRequest
        {
            Name = "Acme LLC",
            Type = EntityType.Business,
            Business = new BusinessEntityMetadata { EntityFormationType = BusinessFormationType.SingleMemberLlc }
        };

        var act = () => service.CreateEntityAsync(OwnerId, request);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.SlugDuplicate);
    }

    [Fact]
    public async Task CreateEntity_GeneratesSlugFromName()
    {
        var request = new CreateEntityRequest
        {
            Name = "Acme Holdings, LLC!",
            Type = EntityType.Business,
            Business = new BusinessEntityMetadata { EntityFormationType = BusinessFormationType.SingleMemberLlc }
        };

        var result = await service.CreateEntityAsync(OwnerId, request);

        result.Slug.Should().Be("acme-holdings-llc");
    }

    // =========================================================================
    // GetEntitiesAsync
    // =========================================================================

    [Fact]
    public async Task GetEntities_ReturnsAllUserEntities()
    {
        await SeedEntityAsync(OwnerId, "Personal", EntityType.Personal);
        await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);
        await SeedEntityAsync(Guid.NewGuid(), "Other User's Trust", EntityType.Trust);

        var results = await service.GetEntitiesAsync(OwnerId, OwnerId);

        results.Should().HaveCount(2);
        results.Select(r => r.Name).Should().BeEquivalentTo("Personal", "Acme LLC");
    }

    [Fact]
    public async Task GetEntities_FilterByType_ReturnsFiltered()
    {
        await SeedEntityAsync(OwnerId, "Personal", EntityType.Personal);
        await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);
        await SeedEntityAsync(OwnerId, "Family Trust", EntityType.Trust);

        var results = await service.GetEntitiesAsync(OwnerId, OwnerId, EntityType.Trust);

        results.Should().HaveCount(1);
        results[0].Type.Should().Be(EntityType.Trust);
        results[0].Name.Should().Be("Family Trust");
    }

    // =========================================================================
    // GetEntityByIdAsync
    // =========================================================================

    [Fact]
    public async Task GetEntityById_Exists_ReturnsDetailDto()
    {
        var entity = await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);

        var result = await service.GetEntityByIdAsync(OwnerId, entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Acme LLC");
        result.Roles.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEntityById_NotFound_ThrowsNotFoundException()
    {
        var act = () => service.GetEntityByIdAsync(OwnerId, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetEntityById_OtherUser_ThrowsNotFoundException()
    {
        var entity = await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);

        var act = () => service.GetEntityByIdAsync(StrangerId, entity.Id);

        // IDOR prevention: return NotFound (not Forbidden) to avoid resource enumeration.
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // =========================================================================
    // UpdateEntityAsync
    // =========================================================================

    [Fact]
    public async Task UpdateEntity_Business_UpdatesMetadata()
    {
        var entity = await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);

        var request = new UpdateEntityRequest
        {
            Name = "Acme Holdings LLC",
            IsActive = true,
            Business = new BusinessEntityMetadata
            {
                EntityFormationType = BusinessFormationType.SingleMemberLlc,
                Ein = "99-8877665"
            }
        };

        var result = await service.UpdateEntityAsync(OwnerId, entity.Id, request);

        result.Name.Should().Be("Acme Holdings LLC");
        result.Business!.Ein.Should().Be("99-8877665");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateEntity_PersonalName_ThrowsBusinessRuleException()
    {
        var personal = await SeedEntityAsync(OwnerId, "Personal", EntityType.Personal);

        var request = new UpdateEntityRequest
        {
            Name = "Renamed Personal",
            IsActive = true
        };

        var act = () => service.UpdateEntityAsync(OwnerId, personal.Id, request);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.PersonalNameImmutable);
    }

    // =========================================================================
    // DeleteEntityAsync
    // =========================================================================

    [Fact]
    public async Task DeleteEntity_Business_Succeeds()
    {
        var entity = await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);

        await service.DeleteEntityAsync(OwnerId, entity.Id);

        var inDb = await dbContext.Entities.FindAsync(entity.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEntity_Personal_ThrowsBusinessRuleException()
    {
        var personal = await SeedEntityAsync(OwnerId, "Personal", EntityType.Personal);

        var act = () => service.DeleteEntityAsync(OwnerId, personal.Id);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be(EntityErrorCodes.PersonalCannotDelete);
    }

    [Fact]
    public async Task DeleteEntity_OtherUser_ThrowsNotFoundException()
    {
        var entity = await SeedEntityAsync(OwnerId, "Acme LLC", EntityType.Business);

        var act = () => service.DeleteEntityAsync(StrangerId, entity.Id);

        // IDOR prevention: return NotFound (not Forbidden) to avoid resource enumeration.
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // =========================================================================
    // Test helpers
    // =========================================================================

    private async Task<Entity> SeedEntityAsync(
        Guid userId,
        string name,
        EntityType type,
        string? slug = null)
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Name = name,
            Slug = slug ?? name.ToLowerInvariant().Replace(' ', '-'),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Entities.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }
}
