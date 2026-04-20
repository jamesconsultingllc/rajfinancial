using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Services.AssetService;

/// <summary>
///     Unit tests for <see cref="Api.Services.AssetService.AssetService"/>.
///     Uses InMemoryDatabase for EF Core and mocked IAuthorizationService.
/// </summary>
public sealed class AssetServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<IAuthorizationService> authMock;
    private readonly Api.Services.AssetService.AssetService service;

    private static readonly Guid OwnerId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid AdvisorId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
    private static readonly Guid StrangerId = Guid.Parse("dddd0000-0000-0000-0000-000000000004");

    public AssetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        dbContext = new ApplicationDbContext(options);
        authMock = new Mock<IAuthorizationService>();
        var logger = new Mock<ILogger<Api.Services.AssetService.AssetService>>();

        // Default: owner always granted
        authMock.Setup(a => a.CheckAccessAsync(OwnerId, OwnerId, DataCategories.Assets, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner));

        // Advisor granted read, denied write
        authMock.Setup(a => a.CheckAccessAsync(AdvisorId, OwnerId, DataCategories.Assets, AccessType.Read))
            .ReturnsAsync(AccessDecision.Grant(AccessDecisionReason.DataAccessGrant, AccessType.Read));
        authMock.Setup(a => a.CheckAccessAsync(AdvisorId, OwnerId, DataCategories.Assets, AccessType.Full))
            .ReturnsAsync(AccessDecision.Deny());

        // Stranger denied everything
        authMock.Setup(a => a.CheckAccessAsync(StrangerId, OwnerId, DataCategories.Assets, It.IsAny<AccessType>()))
            .ReturnsAsync(AccessDecision.Deny());

        service = new Api.Services.AssetService.AssetService(dbContext, authMock.Object, logger.Object);
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }

    // =========================================================================
    // CreateAssetAsync
    // =========================================================================

    [Fact]
    public async Task CreateAsset_BasicAsset_ReturnsDto()
    {
        var request = new CreateAssetRequest
        {
            Name = "Chase Checking",
            Type = AssetType.BankAccount,
            CurrentValue = 5000
        };

        var result = await service.CreateAssetAsync(OwnerId, request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Chase Checking");
        result.Type.Should().Be(AssetType.BankAccount);
        result.CurrentValue.Should().Be(5000);
        result.IsDepreciable.Should().BeFalse();
        result.IsDisposed.Should().BeFalse();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsset_DepreciableAsset_SetsIsDepreciable()
    {
        var request = new CreateAssetRequest
        {
            Name = "Honda Civic",
            Type = AssetType.Vehicle,
            CurrentValue = 25000,
            PurchasePrice = 30000,
            DepreciationMethod = DepreciationMethod.StraightLine,
            UsefulLifeMonths = 60,
            SalvageValue = 5000
        };

        var result = await service.CreateAssetAsync(OwnerId, request);

        result.IsDepreciable.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsset_DepreciationMethodNone_CreatesBaseAsset()
    {
        var request = new CreateAssetRequest
        {
            Name = "Savings",
            Type = AssetType.BankAccount,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.None
        };

        var result = await service.CreateAssetAsync(OwnerId, request);

        result.IsDepreciable.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsset_PersistsToDatabase()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            Type = AssetType.Investment,
            CurrentValue = 1000
        };

        var result = await service.CreateAssetAsync(OwnerId, request);

        var inDb = await dbContext.Assets.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb.Name.Should().Be("Test Asset");
        inDb.UserId.Should().Be(OwnerId);
    }

    // =========================================================================
    // GetAssetsAsync
    // =========================================================================

    [Fact]
    public async Task GetAssets_ReturnsOnlyOwnerAssets()
    {
        var otherId = Guid.NewGuid();
        await SeedAssetAsync(OwnerId, "Owner Asset");
        await SeedAssetAsync(otherId, "Other Asset");

        var results = await service.GetAssetsAsync(OwnerId, OwnerId);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Owner Asset");
    }

    [Fact]
    public async Task GetAssets_ExcludesDisposedByDefault()
    {
        await SeedAssetAsync(OwnerId, "Active");
        await SeedAssetAsync(OwnerId, "Disposed", isDisposed: true);

        var results = await service.GetAssetsAsync(OwnerId, OwnerId);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetAssets_IncludesDisposedWhenRequested()
    {
        await SeedAssetAsync(OwnerId, "Active");
        await SeedAssetAsync(OwnerId, "Disposed", isDisposed: true);

        var results = await service.GetAssetsAsync(OwnerId, OwnerId, includeDisposed: true);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAssets_FiltersByType()
    {
        await SeedAssetAsync(OwnerId, "Checking", assetType: AssetType.BankAccount);
        await SeedAssetAsync(OwnerId, "Car", assetType: AssetType.Vehicle);

        var results = await service.GetAssetsAsync(OwnerId, OwnerId, filterType: AssetType.Vehicle);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Car");
    }

    [Fact]
    public async Task GetAssets_UnauthorizedUser_ThrowsForbidden()
    {
        await SeedAssetAsync(OwnerId, "Asset");

        var act = () => service.GetAssetsAsync(StrangerId, OwnerId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // =========================================================================
    // GetAssetByIdAsync
    // =========================================================================

    [Fact]
    public async Task GetAssetById_ExistingAsset_ReturnsDetailDto()
    {
        var asset = await SeedAssetAsync(OwnerId, "My Asset");

        var result = await service.GetAssetByIdAsync(OwnerId, asset.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("My Asset");
        result.IsDepreciable.Should().BeFalse();
    }

    [Fact]
    public async Task GetAssetById_DepreciableAsset_IncludesComputedFields()
    {
        var asset = await SeedDepreciableAssetAsync(OwnerId, "Car", 30000, 5000, 60,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await service.GetAssetByIdAsync(OwnerId, asset.Id);

        result.Should().NotBeNull();
        result.IsDepreciable.Should().BeTrue();
        result.DepreciationMethod.Should().Be(DepreciationMethod.StraightLine);
        result.AccumulatedDepreciation.Should().NotBeNull();
        result.BookValue.Should().NotBeNull();
        result.MonthlyDepreciation.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAssetById_NotFound_ReturnsNull()
    {
        var result = await service.GetAssetByIdAsync(OwnerId, Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAssetById_UnauthorizedUser_ThrowsForbidden()
    {
        var asset = await SeedAssetAsync(OwnerId, "Asset");

        var act = () => service.GetAssetByIdAsync(StrangerId, asset.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // =========================================================================
    // UpdateAssetAsync
    // =========================================================================

    [Fact]
    public async Task UpdateAsset_UpdatesFields()
    {
        var asset = await SeedAssetAsync(OwnerId, "Old Name");
        var request = new UpdateAssetRequest
        {
            Name = "New Name",
            Type = AssetType.Investment,
            CurrentValue = 99999
        };

        var result = await service.UpdateAssetAsync(OwnerId, asset.Id, request);

        result.Name.Should().Be("New Name");
        result.Type.Should().Be(AssetType.Investment);
        result.CurrentValue.Should().Be(99999);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsset_NotFound_ThrowsNotFound()
    {
        var request = new UpdateAssetRequest { Name = "X", Type = AssetType.BankAccount, CurrentValue = 1 };

        var act = () => service.UpdateAssetAsync(OwnerId, Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsset_UnauthorizedUser_ThrowsForbidden()
    {
        var asset = await SeedAssetAsync(OwnerId, "Asset");
        var request = new UpdateAssetRequest { Name = "X", Type = AssetType.BankAccount, CurrentValue = 1 };

        var act = () => service.UpdateAssetAsync(AdvisorId, asset.Id, request);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateAsset_BaseToDepreciable_SwitchesType()
    {
        var asset = await SeedAssetAsync(OwnerId, "Bank Account");
        var request = new UpdateAssetRequest
        {
            Name = "Now A Car",
            Type = AssetType.Vehicle,
            CurrentValue = 20000,
            PurchasePrice = 25000,
            DepreciationMethod = DepreciationMethod.StraightLine,
            UsefulLifeMonths = 60,
            SalvageValue = 5000
        };

        var result = await service.UpdateAssetAsync(OwnerId, asset.Id, request);

        result.IsDepreciable.Should().BeTrue();
        result.Name.Should().Be("Now A Car");

        // Verify in DB it's actually a DepreciableAsset
        var inDb = await dbContext.Assets.FindAsync(asset.Id);
        inDb.Should().BeOfType<DepreciableAsset>();
    }

    [Fact]
    public async Task UpdateAsset_DepreciableToBase_SwitchesType()
    {
        var asset = await SeedDepreciableAssetAsync(OwnerId, "Car", 30000, 5000, 60,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var request = new UpdateAssetRequest
        {
            Name = "Now A Bank Account",
            Type = AssetType.BankAccount,
            CurrentValue = 5000
        };

        var result = await service.UpdateAssetAsync(OwnerId, asset.Id, request);

        result.IsDepreciable.Should().BeFalse();
        result.Name.Should().Be("Now A Bank Account");

        // Verify in DB it's now a base Asset, not DepreciableAsset
        var inDb = await dbContext.Assets.FindAsync(asset.Id);
        inDb.Should().BeOfType<Asset>();
    }

    [Fact]
    public async Task UpdateAsset_TypeSwitch_PreservesAuditFields()
    {
        var asset = await SeedAssetAsync(OwnerId, "Original");
        var originalCreatedAt = asset.CreatedAt;

        var request = new UpdateAssetRequest
        {
            Name = "Switched",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.DecliningBalance,
            UsefulLifeMonths = 48,
            SalvageValue = 2000
        };

        var result = await service.UpdateAssetAsync(OwnerId, asset.Id, request);

        result.CreatedAt.Should().Be(originalCreatedAt.UtcDateTime);
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // =========================================================================
    // DeleteAssetAsync
    // =========================================================================

    [Fact]
    public async Task DeleteAsset_RemovesFromDatabase()
    {
        var asset = await SeedAssetAsync(OwnerId, "To Delete");

        await service.DeleteAssetAsync(OwnerId, asset.Id);

        var inDb = await dbContext.Assets.FindAsync(asset.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsset_NotFound_ThrowsNotFound()
    {
        var act = () => service.DeleteAssetAsync(OwnerId, Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsset_UnauthorizedUser_ThrowsForbidden()
    {
        var asset = await SeedAssetAsync(OwnerId, "Asset");

        var act = () => service.DeleteAssetAsync(AdvisorId, asset.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // =========================================================================
    // Advisor (read-only) access
    // =========================================================================

    [Fact]
    public async Task GetAssets_AdvisorWithReadAccess_Succeeds()
    {
        await SeedAssetAsync(OwnerId, "Owner Asset");

        var results = await service.GetAssetsAsync(AdvisorId, OwnerId);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Owner Asset");
    }

    [Fact]
    public async Task GetAssetById_AdvisorWithReadAccess_Succeeds()
    {
        var asset = await SeedAssetAsync(OwnerId, "Owner Asset");

        var result = await service.GetAssetByIdAsync(AdvisorId, asset.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Owner Asset");
    }

    // =========================================================================
    // Ordering
    // =========================================================================

    [Fact]
    public async Task GetAssets_ReturnsOrderedByName()
    {
        await SeedAssetAsync(OwnerId, "Zebra Fund");
        await SeedAssetAsync(OwnerId, "Alpha Account");
        await SeedAssetAsync(OwnerId, "Middle Investment");

        var results = await service.GetAssetsAsync(OwnerId, OwnerId);

        results.Should().HaveCount(3);
        results[0].Name.Should().Be("Alpha Account");
        results[1].Name.Should().Be("Middle Investment");
        results[2].Name.Should().Be("Zebra Fund");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<Asset> SeedAssetAsync(
        Guid userId, string name,
        AssetType assetType = AssetType.BankAccount,
        bool isDisposed = false)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = assetType,
            CurrentValue = 1000,
            IsDisposed = isDisposed,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset;
    }

    private async Task<DepreciableAsset> SeedDepreciableAssetAsync(
        Guid userId, string name,
        decimal purchasePrice, decimal salvageValue,
        int usefulLifeMonths, DateTimeOffset inServiceDate)
    {
        var asset = new DepreciableAsset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = AssetType.Vehicle,
            CurrentValue = purchasePrice,
            PurchasePrice = purchasePrice,
            DepreciationMethod = DepreciationMethod.StraightLine,
            SalvageValue = salvageValue,
            UsefulLifeMonths = usefulLifeMonths,
            InServiceDate = inServiceDate,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset;
    }
}
