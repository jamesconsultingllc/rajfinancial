// ============================================================================
// RAJ Financial - AssetFunctions Unit Tests
// ============================================================================
// Tests the Azure Functions HTTP layer in isolation. The service layer
// (EF Core + authorization) is tested separately in AssetServiceTests.
// These tests verify:
//   - Parameter parsing (query strings, route params)
//   - Proper delegation to IAssetService
//   - HTTP status codes (200, 201, 204, 401)
//   - Error handling (invalid GUID → NotFoundException)
// ============================================================================

using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Functions.Assets;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.AssetService;
using RajFinancial.Api.Tests.Middleware;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Tests.Functions.Assets;

/// <summary>
///     Unit tests for <see cref="AssetFunctions"/>.
///     Verifies HTTP layer behavior: routing, status codes, delegation to service.
/// </summary>
public class AssetFunctionsTests
{
    private static readonly Guid TestUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid TestAssetId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

    private readonly Mock<IAssetService> assetServiceMock = new();
    private readonly Mock<ISerializationFactory> serializationFactoryMock = new();
    private readonly Mock<ILogger<AssetFunctions>> loggerMock = new();
    private readonly AssetFunctions sut;

    public AssetFunctionsTests()
    {
        sut = new AssetFunctions(
            assetServiceMock.Object,
            serializationFactoryMock.Object,
            loggerMock.Object);
    }

    // =========================================================================
    // GetAssets
    // =========================================================================

    [Fact]
    public async Task GetAssets_AuthenticatedUser_DelegatesToService()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);
        var expectedAssets = new List<AssetDto>
        {
            CreateAssetDto("Asset 1"),
            CreateAssetDto("Asset 2")
        };

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, null, false))
            .ReturnsAsync(expectedAssets);

        SetupSerializationResponse(context, req);

        // Act
        var response = await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, null, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_WithOwnerUserIdParam_PassesOwnerToService()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "ownerUserId", otherUserId.ToString() } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, otherUserId, null, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, otherUserId, null, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_WithTypeFilter_ParsesEnumCorrectly()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "type", "Vehicle" } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, AssetType.Vehicle, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, AssetType.Vehicle, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_WithTypeFilterAsInteger_ParsesCorrectly()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "type", "1" } }; // 1 = Vehicle
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, AssetType.Vehicle, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, AssetType.Vehicle, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_WithIncludeDisposed_ParsesBoolCorrectly()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "includeDisposed", "true" } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, null, true))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, null, true),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_WithInvalidType_PassesNullFilter()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "type", "InvalidType" } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, null, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, null, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = CreateUnauthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.GetAssets(req.Object, context);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // GetAssetById
    // =========================================================================

    [Fact]
    public async Task GetAssetById_ValidId_DelegatesToService()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);
        var detail = CreateAssetDetailDto();

        assetServiceMock
            .Setup(s => s.GetAssetByIdAsync(TestUserId, TestAssetId))
            .ReturnsAsync(detail);

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssetById(req.Object, context, TestAssetId.ToString());

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetByIdAsync(TestUserId, TestAssetId),
            Times.Once);
    }

    [Fact]
    public async Task GetAssetById_InvalidGuid_ThrowsNotFound()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.GetAssetById(req.Object, context, "not-a-guid");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAssetById_NotFound_ThrowsNotFound()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);

        assetServiceMock
            .Setup(s => s.GetAssetByIdAsync(TestUserId, TestAssetId))
            .ReturnsAsync((AssetDetailDto?)null);

        // Act
        var act = () => sut.GetAssetById(req.Object, context, TestAssetId.ToString());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAssetById_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = CreateUnauthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.GetAssetById(req.Object, context, TestAssetId.ToString());

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // CreateAsset
    // =========================================================================

    [Fact]
    public async Task CreateAsset_ValidRequest_DelegatesToService()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var createRequest = new CreateAssetRequest
        {
            Name = "New Asset",
            Type = AssetType.BankAccount,
            CurrentValue = 5000
        };
        context.WithRequestBody(createRequest);

        var req = CreateMockRequest(context);
        var createdDto = CreateAssetDto("New Asset");

        assetServiceMock
            .Setup(s => s.CreateAssetAsync(TestUserId, It.IsAny<CreateAssetRequest>()))
            .ReturnsAsync(createdDto);

        SetupSerializationResponse(context, req);

        // Act
        await sut.CreateAsset(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.CreateAssetAsync(TestUserId, It.IsAny<CreateAssetRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsset_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = CreateUnauthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.CreateAsset(req.Object, context);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // UpdateAsset
    // =========================================================================

    [Fact]
    public async Task UpdateAsset_ValidRequest_DelegatesToService()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var updateRequest = new UpdateAssetRequest
        {
            Name = "Updated Asset",
            Type = AssetType.Investment,
            CurrentValue = 75_000
        };
        context.WithRequestBody(updateRequest);

        var req = CreateMockRequest(context);
        var updatedDto = CreateAssetDto("Updated Asset");

        assetServiceMock
            .Setup(s => s.UpdateAssetAsync(TestUserId, TestAssetId, It.IsAny<UpdateAssetRequest>()))
            .ReturnsAsync(updatedDto);

        SetupSerializationResponse(context, req);

        // Act
        await sut.UpdateAsset(req.Object, context, TestAssetId.ToString());

        // Assert
        assetServiceMock.Verify(
            s => s.UpdateAssetAsync(TestUserId, TestAssetId, It.IsAny<UpdateAssetRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsset_InvalidGuid_ThrowsNotFound()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.UpdateAsset(req.Object, context, "bad-id");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsset_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = CreateUnauthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.UpdateAsset(req.Object, context, TestAssetId.ToString());

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // DeleteAsset
    // =========================================================================

    [Fact]
    public async Task DeleteAsset_ValidId_ReturnsNoContent()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);

        assetServiceMock
            .Setup(s => s.DeleteAssetAsync(TestUserId, TestAssetId))
            .Returns(Task.CompletedTask);

        // Act
        var response = await sut.DeleteAsset(req.Object, context, TestAssetId.ToString());

        // Assert
        assetServiceMock.Verify(
            s => s.DeleteAssetAsync(TestUserId, TestAssetId),
            Times.Once);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAsset_InvalidGuid_ThrowsNotFound()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.DeleteAsset(req.Object, context, "invalid");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsset_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var context = CreateUnauthenticatedContext();
        var req = CreateMockRequest(context);

        // Act
        var act = () => sut.DeleteAsset(req.Object, context, TestAssetId.ToString());

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // Query Parameter Edge Cases
    // =========================================================================

    [Fact]
    public async Task GetAssets_WithAllQueryParams_AllParsedCorrectly()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection
        {
            { "ownerUserId", otherUserId.ToString() },
            { "type", "RealEstate" },
            { "includeDisposed", "true" }
        };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, otherUserId, AssetType.RealEstate, true))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, otherUserId, AssetType.RealEstate, true),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_IncludeDisposedNotBoolean_DefaultsToFalse()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "includeDisposed", "notabool" } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, null, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, null, false),
            Times.Once);
    }

    [Fact]
    public async Task GetAssets_OwnerUserIdNotAGuid_DefaultsToAuthUser()
    {
        // Arrange
        var context = CreateAuthenticatedContext();
        var query = new NameValueCollection { { "ownerUserId", "not-a-guid" } };
        var req = CreateMockRequest(context, query);

        assetServiceMock
            .Setup(s => s.GetAssetsAsync(TestUserId, TestUserId, null, false))
            .ReturnsAsync(new List<AssetDto>());

        SetupSerializationResponse(context, req);

        // Act
        await sut.GetAssets(req.Object, context);

        // Assert
        assetServiceMock.Verify(
            s => s.GetAssetsAsync(TestUserId, TestUserId, null, false),
            Times.Once);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    ///     Creates a <see cref="TestFunctionContext"/> configured as an authenticated user.
    /// </summary>
    private static TestFunctionContext CreateAuthenticatedContext() =>
        new TestFunctionContext().WithAuthentication(TestUserId);

    /// <summary>
    ///     Creates a <see cref="TestFunctionContext"/> with no authentication.
    ///     <c>GetUserIdAsGuid()</c> will return null, triggering <see cref="UnauthorizedException"/>.
    /// </summary>
    private static TestFunctionContext CreateUnauthenticatedContext() => new();

    /// <summary>
    ///     Creates a mock <see cref="HttpRequestData"/> with optional query parameters
    ///     using the context's <see cref="TestFunctionContext.CreateMockHttpRequest"/> builder.
    /// </summary>
    private static Mock<HttpRequestData> CreateMockRequest(
        TestFunctionContext context,
        NameValueCollection? queryParams = null) =>
        context.CreateMockHttpRequest(queryParams);

    /// <summary>
    ///     Sets up the <c>CreateSerializedResponseAsync</c> extension method dependencies.
    ///     Configures response content type and serialization factory to return valid bytes.
    /// </summary>
    private void SetupSerializationResponse(
        TestFunctionContext context,
        Mock<HttpRequestData> mockRequest)
    {
        context.WithResponseContentType();

        // Mock the serialization factory to return valid bytes
        serializationFactoryMock
            .Setup(f => f.SerializeAsync(It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync("{}"u8.ToArray());
    }

    /// <summary>
    ///     Creates a test <see cref="AssetDto"/> with sensible defaults.
    /// </summary>
    private static AssetDto CreateAssetDto(string name = "Test Asset") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = AssetType.BankAccount,
        CurrentValue = 5_000m,
        IsDepreciable = false,
        IsDisposed = false,
        HasBeneficiaries = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    ///     Creates a test <see cref="AssetDetailDto"/> with sensible defaults.
    /// </summary>
    private static AssetDetailDto CreateAssetDetailDto() => new()
    {
        Id = TestAssetId,
        Name = "Detail Asset",
        Type = AssetType.BankAccount,
        CurrentValue = 10_000m,
        IsDepreciable = false,
        IsDisposed = false,
        HasBeneficiaries = false,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
