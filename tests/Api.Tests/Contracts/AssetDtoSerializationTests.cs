// ============================================================================
// RAJ Financial - Asset DTO MemoryPack Round-Trip Tests
// ============================================================================
// Validates MemoryPack serialization for all Asset DTOs and request contracts.
// Each DTO must survive a serialize → deserialize round-trip with all
// properties intact. This guards against MemoryPackOrder mis-numbering,
// missing [MemoryPackable] attributes, and version-tolerant schema drift.
// ============================================================================

using FluentAssertions;
using MemoryPack;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Tests.Contracts;

/// <summary>
///     MemoryPack round-trip serialization tests for Asset contracts.
/// </summary>
public class AssetDtoSerializationTests
{
    // =========================================================================
    // AssetDto
    // =========================================================================

    [Fact]
    public void AssetDto_MinimalFields_RoundTrips()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "Chase Checking",
            Type = AssetType.BankAccount,
            CurrentValue = 5_000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void AssetDto_AllFields_RoundTrips()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "Investment Property",
            Type = AssetType.RealEstate,
            CurrentValue = 250_000d,
            PurchasePrice = 200_000d,
            PurchaseDate = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Rental property in Austin, TX",
            Location = "123 Main St, Austin TX 78701",
            AccountNumber = "PROP-001",
            InstitutionName = "First National Bank",
            IsDepreciable = true,
            IsDisposed = false,
            HasBeneficiaries = true,
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Theory]
    [InlineData(AssetType.RealEstate)]
    [InlineData(AssetType.Vehicle)]
    [InlineData(AssetType.Investment)]
    [InlineData(AssetType.Retirement)]
    [InlineData(AssetType.BankAccount)]
    [InlineData(AssetType.Insurance)]
    [InlineData(AssetType.Business)]
    [InlineData(AssetType.PersonalProperty)]
    [InlineData(AssetType.Collectible)]
    [InlineData(AssetType.Cryptocurrency)]
    [InlineData(AssetType.IntellectualProperty)]
    [InlineData(AssetType.Other)]
    public void AssetDto_AllAssetTypes_RoundTrip(AssetType type)
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = $"Asset of type {type}",
            Type = type,
            CurrentValue = 1_000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Type.Should().Be(type);
    }

    [Fact]
    public void AssetDto_LargeDecimalValues_RoundTrip()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "High-value asset",
            Type = AssetType.RealEstate,
            CurrentValue = 999_999_999.99d,
            PurchasePrice = 123_456_789.01d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.CurrentValue.Should().Be(999_999_999.99d);
        result.PurchasePrice.Should().Be(123_456_789.01d);
    }

    // =========================================================================
    // AssetDetailDto
    // =========================================================================

    [Fact]
    public void AssetDetailDto_MinimalFields_RoundTrips()
    {
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Simple Account",
            Type = AssetType.BankAccount,
            CurrentValue = 10_000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void AssetDetailDto_AllFields_RoundTrips()
    {
        var now = DateTime.UtcNow;
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Rental Duplex",
            Type = AssetType.RealEstate,
            CurrentValue = 350_000d,
            PurchasePrice = 280_000d,
            PurchaseDate = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Description = "Two-unit duplex in downtown area",
            Location = "456 Oak Ave, Portland OR",
            AccountNumber = "RE-2020-001",
            InstitutionName = "Pacific Mortgage",
            IsDepreciable = true,
            DepreciationMethod = DepreciationMethod.StraightLine,
            SalvageValue = 30_000d,
            UsefulLifeMonths = 330,
            InServiceDate = new DateTime(2020, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            AccumulatedDepreciation = 45_000d,
            BookValue = 235_000d,
            MonthlyDepreciation = 757.58d,
            DepreciationPercentComplete = 0.18d,
            IsDisposed = false,
            DisposalDate = null,
            DisposalPrice = null,
            DisposalNotes = null,
            MarketValue = 375_000d,
            LastValuationDate = now,
            HasBeneficiaries = true,
            Beneficiaries =
            [
                new BeneficiaryAssignmentDto
                {
                    BeneficiaryId = Guid.NewGuid(),
                    BeneficiaryName = "Jane Doe",
                    Relationship = "Spouse",
                    AllocationPercent = 60d,
                    Type = "Primary"
                },
                new BeneficiaryAssignmentDto
                {
                    BeneficiaryId = Guid.NewGuid(),
                    BeneficiaryName = "John Jr.",
                    Relationship = "Child",
                    AllocationPercent = 40d,
                    Type = "Contingent"
                }
            ],
            CreatedAt = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = now
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void AssetDetailDto_DisposedAsset_RoundTrips()
    {
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Sold Vehicle",
            Type = AssetType.Vehicle,
            CurrentValue = 0d,
            IsDepreciable = true,
            DepreciationMethod = DepreciationMethod.DecliningBalance,
            IsDisposed = true,
            DisposalDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DisposalPrice = 12_500d,
            DisposalNotes = "Traded in at dealer",
            HasBeneficiaries = false,
            CreatedAt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = RoundTrip(original);

        result.IsDisposed.Should().BeTrue();
        result.DisposalDate.Should().Be(original.DisposalDate);
        result.DisposalPrice.Should().Be(12_500d);
        result.DisposalNotes.Should().Be("Traded in at dealer");
    }

    [Theory]
    [InlineData(DepreciationMethod.None)]
    [InlineData(DepreciationMethod.StraightLine)]
    [InlineData(DepreciationMethod.DecliningBalance)]
    [InlineData(DepreciationMethod.Macrs)]
    public void AssetDetailDto_AllDepreciationMethods_RoundTrip(DepreciationMethod method)
    {
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = $"Asset ({method})",
            Type = AssetType.Vehicle,
            CurrentValue = 25_000d,
            IsDepreciable = method != DepreciationMethod.None,
            DepreciationMethod = method,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.DepreciationMethod.Should().Be(method);
    }

    // =========================================================================
    // BeneficiaryAssignmentDto
    // =========================================================================

    [Fact]
    public void BeneficiaryAssignmentDto_RoundTrips()
    {
        var original = new BeneficiaryAssignmentDto
        {
            BeneficiaryId = Guid.NewGuid(),
            BeneficiaryName = "Alice Smith",
            Relationship = "Spouse",
            AllocationPercent = 100d,
            Type = "Primary"
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33.33)]
    [InlineData(50)]
    [InlineData(100)]
    public void BeneficiaryAssignmentDto_VariousAllocations_RoundTrip(double percent)
    {
        var original = new BeneficiaryAssignmentDto
        {
            BeneficiaryId = Guid.NewGuid(),
            BeneficiaryName = "Test",
            Relationship = "Other",
            AllocationPercent = percent,
            Type = "Primary"
        };

        var result = RoundTrip(original);

        result.AllocationPercent.Should().Be(percent);
    }

    // =========================================================================
    // CreateAssetRequest
    // =========================================================================

    [Fact]
    public void CreateAssetRequest_MinimalFields_RoundTrips()
    {
        var original = new CreateAssetRequest
        {
            Name = "New Savings",
            Type = AssetType.BankAccount,
            CurrentValue = 500d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CreateAssetRequest_AllFields_RoundTrips()
    {
        var original = new CreateAssetRequest
        {
            Name = "Commercial Building",
            Type = AssetType.RealEstate,
            CurrentValue = 1_200_000d,
            PurchasePrice = 1_000_000d,
            PurchaseDate = new DateTime(2021, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Three-story commercial building",
            Location = "789 Commerce Blvd, Seattle WA",
            AccountNumber = "COMM-2021-003",
            InstitutionName = "US Bank",
            DepreciationMethod = DepreciationMethod.Macrs,
            SalvageValue = 100_000d,
            UsefulLifeMonths = 468,
            InServiceDate = new DateTime(2021, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            MarketValue = 1_350_000d,
            LastValuationDate = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // UpdateAssetRequest
    // =========================================================================

    [Fact]
    public void UpdateAssetRequest_MinimalFields_RoundTrips()
    {
        var original = new UpdateAssetRequest
        {
            Name = "Renamed Account",
            Type = AssetType.BankAccount,
            CurrentValue = 7_500d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void UpdateAssetRequest_AllFields_RoundTrips()
    {
        var original = new UpdateAssetRequest
        {
            Name = "Updated Property",
            Type = AssetType.RealEstate,
            CurrentValue = 300_000d,
            PurchasePrice = 250_000d,
            PurchaseDate = new DateTime(2019, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Updated description",
            Location = "Updated location",
            AccountNumber = "UPD-001",
            InstitutionName = "Updated Bank",
            DepreciationMethod = DepreciationMethod.StraightLine,
            SalvageValue = 50_000d,
            UsefulLifeMonths = 240,
            InServiceDate = new DateTime(2019, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            MarketValue = 320_000d,
            LastValuationDate = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // Edge Cases
    // =========================================================================

    [Fact]
    public void AssetDto_NullOptionalFields_RoundTrips()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "Minimal",
            Type = AssetType.Other,
            CurrentValue = 0d,
            PurchasePrice = null,
            PurchaseDate = null,
            Description = null,
            Location = null,
            AccountNumber = null,
            InstitutionName = null,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        var result = RoundTrip(original);

        result.PurchasePrice.Should().BeNull();
        result.PurchaseDate.Should().BeNull();
        result.Description.Should().BeNull();
        result.Location.Should().BeNull();
        result.AccountNumber.Should().BeNull();
        result.InstitutionName.Should().BeNull();
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void AssetDetailDto_EmptyBeneficiariesList_RoundTrips()
    {
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "No Beneficiaries",
            Type = AssetType.BankAccount,
            CurrentValue = 1_000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            Beneficiaries = [],
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Beneficiaries.Should().BeEmpty();
    }

    [Fact]
    public void AssetDetailDto_MultipleBeneficiaries_RoundTrips()
    {
        var beneficiaries = Enumerable.Range(1, 5)
            .Select(i => new BeneficiaryAssignmentDto
            {
                BeneficiaryId = Guid.NewGuid(),
                BeneficiaryName = $"Beneficiary {i}",
                Relationship = i % 2 == 0 ? "Child" : "Spouse",
                AllocationPercent = 20d,
                Type = i == 1 ? "Primary" : "Contingent"
            })
            .ToArray();

        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Multi-Beneficiary Asset",
            Type = AssetType.Insurance,
            CurrentValue = 500_000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = true,
            Beneficiaries = beneficiaries,
            CreatedAt = DateTime.UtcNow
        };

        var result = RoundTrip(original);

        result.Beneficiaries.Should().HaveCount(5);
        result.Beneficiaries.Should().BeEquivalentTo(beneficiaries);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    ///     Serializes and deserializes a value using MemoryPack binary format.
    /// </summary>
    private static T RoundTrip<T>(T value)
    {
        var bytes = MemoryPackSerializer.Serialize(value);
        bytes.Should().NotBeEmpty("serialization should produce output");

        var result = MemoryPackSerializer.Deserialize<T>(bytes);
        result.Should().NotBeNull("deserialization should succeed");

        return result!;
    }
}
