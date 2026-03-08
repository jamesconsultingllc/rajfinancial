// ============================================================================
// RAJ Financial - Asset Metadata MemoryPack Round-Trip Tests
// ============================================================================
// Validates MemoryPack serialization for all per-type metadata records and
// the IAssetMetadata union interface. Each metadata type must survive a
// serialize → deserialize round-trip with all properties intact, both as
// a standalone record and as a polymorphic IAssetMetadata union.
// ============================================================================

using FluentAssertions;
using MemoryPack;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Contracts.Assets.Metadata;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Contracts;

/// <summary>
///     MemoryPack round-trip serialization tests for per-type asset metadata.
/// </summary>
public class AssetMetadataSerializationTests
{
    // =========================================================================
    // VehicleMetadata
    // =========================================================================

    [Fact]
    public void VehicleMetadata_AllFields_RoundTrips()
    {
        var original = new VehicleMetadata
        {
            Vin = "1HGCM82633A004352",
            Make = "Tesla",
            Model = "Model 3",
            Year = 2024,
            Mileage = 12500,
            Color = "Midnight Silver",
            LicensePlate = "ABC1234"
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void VehicleMetadata_MinimalFields_RoundTrips()
    {
        var original = new VehicleMetadata
        {
            Make = "Ford",
            Model = "F-150",
            Year = 2023
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void VehicleMetadata_UnionRoundTrip()
    {
        var original = new VehicleMetadata
        {
            Make = "BMW",
            Model = "X5",
            Year = 2025
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<VehicleMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // RealEstateMetadata
    // =========================================================================

    [Fact]
    public void RealEstateMetadata_AllFields_RoundTrips()
    {
        var original = new RealEstateMetadata
        {
            Address = "123 Main St",
            Address2 = "Suite 100",
            City = "Austin",
            State = "TX",
            ZipCode = "78701",
            Country = "US",
            PropertyType = PropertyType.SingleFamily,
            SquareFeet = 2400,
            YearBuilt = 1995,
            LotSize = "0.25 acres",
            Bedrooms = 4,
            Bathrooms = 2.5
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void RealEstateMetadata_UnionRoundTrip()
    {
        var original = new RealEstateMetadata
        {
            Address = "456 Oak Ave",
            City = "Portland",
            State = "OR",
            ZipCode = "97201",
            PropertyType = PropertyType.Condo
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<RealEstateMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // InvestmentMetadata
    // =========================================================================

    [Fact]
    public void InvestmentMetadata_WithHoldings_RoundTrips()
    {
        var original = new InvestmentMetadata
        {
            AccountType = InvestmentAccountType.Individual,
            Holdings =
            [
                new Holding
                {
                    Ticker = "AAPL",
                    Name = "Apple Inc.",
                    HoldingType = HoldingType.Stocks,
                    Shares = 100,
                    CostBasis = 15000d,
                    CurrentPrice = 175.50d
                },
                new Holding
                {
                    Ticker = "VTSAX",
                    HoldingType = HoldingType.MutualFunds,
                    Shares = 50.5
                }
            ]
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void InvestmentMetadata_RSU_RoundTrips()
    {
        var original = new InvestmentMetadata
        {
            AccountType = InvestmentAccountType.RSU,
            GrantDate = new DateTime(2023, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            TotalSharesGranted = 1000,
            SharesVested = 250,
            VestingSchedule =
            [
                new VestingEvent
                {
                    Date = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    Shares = 250
                },
                new VestingEvent
                {
                    Date = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    Shares = 250
                }
            ],
            Ticker = "MSFT",
            GrantPricePerShare = 350.00d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void InvestmentMetadata_UnionRoundTrip()
    {
        var original = new InvestmentMetadata
        {
            AccountType = InvestmentAccountType.Joint,
            Holdings =
            [
                new Holding
                {
                    Ticker = "SPY",
                    HoldingType = HoldingType.ETF,
                    Shares = 200
                }
            ]
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<InvestmentMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // RetirementMetadata
    // =========================================================================

    [Fact]
    public void RetirementMetadata_AllFields_RoundTrips()
    {
        var original = new RetirementMetadata
        {
            AccountType = RetirementAccountType.Plan401k,
            EmployerMatchTiers =
            [
                new EmployerMatchTier { MatchPercent = 100, OnFirst = 6.0 },
                new EmployerMatchTier { MatchPercent = 50, OnFirst = 2.0 }
            ],
            VestingPercent = 80,
            VestingScheduleMonths = 48,
            ProjectedAnnualContribution = 23000d,
            Salary = 150000d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void RetirementMetadata_UnionRoundTrip()
    {
        var original = new RetirementMetadata
        {
            AccountType = RetirementAccountType.RothIRA
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<RetirementMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // BankAccountMetadata
    // =========================================================================

    [Fact]
    public void BankAccountMetadata_AllFields_RoundTrips()
    {
        var original = new BankAccountMetadata
        {
            BankAccountType = BankAccountType.CD,
            RoutingNumber = "021000021",
            Apy = 5.25,
            MaturityDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Term = 12,
            IsJointAccount = false
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void BankAccountMetadata_UnionRoundTrip()
    {
        var original = new BankAccountMetadata
        {
            BankAccountType = BankAccountType.HYSA,
            Apy = 4.5
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<BankAccountMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // InsuranceMetadata
    // =========================================================================

    [Fact]
    public void InsuranceMetadata_WholeLife_RoundTrips()
    {
        var original = new InsuranceMetadata
        {
            PolicyType = InsurancePolicyType.WholeLife,
            CashValue = 45000d,
            DeathBenefit = 500000d,
            PremiumAmount = 350d,
            PremiumFrequency = PremiumFrequency.Monthly,
            PolicyStartDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Riders =
            [
                new PolicyRider
                {
                    RiderType = PolicyRiderType.PaidUpAdditions,
                    Value = 12000d,
                    AnnualCost = 1200d
                },
                new PolicyRider
                {
                    RiderType = PolicyRiderType.WaiverOfPremium,
                    AnnualCost = 50d
                }
            ],
            DividendOption = DividendOption.PaidUpAdditions,
            AnnualDividend = 2500d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void InsuranceMetadata_TermLife_RoundTrips()
    {
        var original = new InsuranceMetadata
        {
            PolicyType = InsurancePolicyType.TermLife,
            DeathBenefit = 1000000d,
            PremiumAmount = 75d,
            PremiumFrequency = PremiumFrequency.Monthly,
            PolicyStartDate = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PolicyEndDate = new DateTime(2040, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void InsuranceMetadata_UnionRoundTrip()
    {
        var original = new InsuranceMetadata
        {
            PolicyType = InsurancePolicyType.Annuity,
            CashValue = 100000d
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<InsuranceMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // BusinessMetadata
    // =========================================================================

    [Fact]
    public void BusinessMetadata_AllFields_RoundTrips()
    {
        var original = new BusinessMetadata
        {
            EntityType = BusinessEntityType.LLC,
            OwnershipPercent = 51.0,
            Ein = "12-3456789",
            NaicsCode = "541511",
            DunsNumber = "123456789",
            Industry = "Software Development",
            AnnualRevenue = 2500000d,
            NumberOfEmployees = 25,
            FoundedDate = new DateTime(2018, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Registrations =
            [
                new StateRegistration
                {
                    State = "DE",
                    SosNumber = "7654321",
                    FilingDate = new DateTime(2018, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsFormationState = true
                },
                new StateRegistration
                {
                    State = "TX",
                    SosNumber = "8765432",
                    FilingDate = new DateTime(2018, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsFormationState = false
                }
            ]
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void BusinessMetadata_UnionRoundTrip()
    {
        var original = new BusinessMetadata
        {
            EntityType = BusinessEntityType.SCorp,
            OwnershipPercent = 100
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<BusinessMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // PersonalPropertyMetadata
    // =========================================================================

    [Fact]
    public void PersonalPropertyMetadata_AllFields_RoundTrips()
    {
        var original = new PersonalPropertyMetadata
        {
            Category = PersonalPropertyCategory.Jewelry,
            Condition = ItemCondition.Excellent,
            SerialNumber = "SN-123456",
            Brand = "Rolex",
            ModelNumber = "116610LN",
            AppraiserName = "GIA Certified",
            LastAppraisalDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            InsuredValue = 12000d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PersonalPropertyMetadata_OtherCategory_RoundTrips()
    {
        var original = new PersonalPropertyMetadata
        {
            Category = PersonalPropertyCategory.Other,
            CustomCategory = "Artwork supplies",
            Condition = ItemCondition.Good
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PersonalPropertyMetadata_UnionRoundTrip()
    {
        var original = new PersonalPropertyMetadata
        {
            Category = PersonalPropertyCategory.Electronics,
            Brand = "Apple",
            ModelNumber = "MacBook Pro 16\""
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<PersonalPropertyMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // CollectibleMetadata
    // =========================================================================

    [Fact]
    public void CollectibleMetadata_AllFields_RoundTrips()
    {
        var original = new CollectibleMetadata
        {
            Category = CollectibleCategory.TradingCards,
            Condition = ItemCondition.Mint,
            Provenance = "Purchased from original owner",
            SerialNumber = "PSA-12345678",
            CertificationBody = "PSA",
            CertificationNumber = "12345678",
            Grade = "PSA 10",
            Edition = "1st Edition",
            Artist = null,
            AppraiserName = "Card Authenticators Inc.",
            LastAppraisalDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            InsuredValue = 50000d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CollectibleMetadata_UnionRoundTrip()
    {
        var original = new CollectibleMetadata
        {
            Category = CollectibleCategory.Art,
            Artist = "Banksy",
            Condition = ItemCondition.Excellent
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<CollectibleMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // CryptocurrencyMetadata
    // =========================================================================

    [Fact]
    public void CryptocurrencyMetadata_AllFields_RoundTrips()
    {
        var original = new CryptocurrencyMetadata
        {
            WalletType = CryptoWalletType.HardwareWallet,
            Holdings =
            [
                new CryptoHolding
                {
                    CoinSymbol = "BTC",
                    CoinName = "Bitcoin",
                    Quantity = 1.5,
                    CostBasis = 45000d,
                    CurrentPrice = 65000d,
                    IsStaking = false
                },
                new CryptoHolding
                {
                    CoinSymbol = "ETH",
                    CoinName = "Ethereum",
                    Quantity = 32.0,
                    CostBasis = 48000d,
                    CurrentPrice = 3500d,
                    IsStaking = true,
                    StakingApy = 4.2
                }
            ]
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void CryptocurrencyMetadata_UnionRoundTrip()
    {
        var original = new CryptocurrencyMetadata
        {
            WalletType = CryptoWalletType.Exchange,
            Holdings =
            [
                new CryptoHolding
                {
                    CoinSymbol = "SOL",
                    Quantity = 500
                }
            ]
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<CryptocurrencyMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // IntellectualPropertyMetadata
    // =========================================================================

    [Fact]
    public void IntellectualPropertyMetadata_AllFields_RoundTrips()
    {
        var original = new IntellectualPropertyMetadata
        {
            IpType = IpType.Patent,
            RegistrationNumber = "US10,123,456",
            Jurisdiction = "US",
            FilingDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            IssueDate = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpirationDate = new DateTime(2040, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = IpStatus.Active,
            Licensee = "TechCorp Inc.",
            RoyaltyRate = 5.0,
            AnnualRevenue = 250000d
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void IntellectualPropertyMetadata_UnionRoundTrip()
    {
        var original = new IntellectualPropertyMetadata
        {
            IpType = IpType.Trademark,
            Status = IpStatus.Pending,
            Jurisdiction = "US"
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<IntellectualPropertyMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // OtherAssetMetadata
    // =========================================================================

    [Fact]
    public void OtherAssetMetadata_AllFields_RoundTrips()
    {
        var original = new OtherAssetMetadata
        {
            Category = "Livestock",
            CustomFields =
            [
                new CustomField { Key = "breed", Value = "Angus" },
                new CustomField { Key = "headCount", Value = "150" }
            ]
        };

        var result = RoundTrip(original);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void OtherAssetMetadata_NullFields_RoundTrips()
    {
        var original = new OtherAssetMetadata();

        var result = RoundTrip(original);

        result.Category.Should().BeNull();
        result.CustomFields.Should().BeNull();
    }

    [Fact]
    public void OtherAssetMetadata_UnionRoundTrip()
    {
        var original = new OtherAssetMetadata
        {
            Category = "Miscellaneous"
        };

        var result = RoundTripUnion(original);

        result.Should().BeOfType<OtherAssetMetadata>()
            .Which.Should().BeEquivalentTo(original);
    }

    // =========================================================================
    // Metadata on DTOs
    // =========================================================================

    [Fact]
    public void AssetDto_WithMetadata_RoundTrips()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "2024 Tesla Model 3",
            Type = AssetType.Vehicle,
            CurrentValue = 35000d,
            IsDepreciable = true,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow,
            Metadata = new VehicleMetadata
            {
                Make = "Tesla",
                Model = "Model 3",
                Year = 2024,
                Vin = "5YJ3E1EA0RF123456"
            }
        };

        var result = RoundTrip(original);

        result.Metadata.Should().BeOfType<VehicleMetadata>()
            .Which.Should().BeEquivalentTo((VehicleMetadata)original.Metadata!);
    }

    [Fact]
    public void AssetDto_WithNullMetadata_RoundTrips()
    {
        var original = new AssetDto
        {
            Id = Guid.NewGuid(),
            Name = "Simple Asset",
            Type = AssetType.Other,
            CurrentValue = 1000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow,
            Metadata = null
        };

        var result = RoundTrip(original);

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void AssetDetailDto_WithMetadata_RoundTrips()
    {
        var original = new AssetDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Downtown Condo",
            Type = AssetType.RealEstate,
            CurrentValue = 450000d,
            IsDepreciable = false,
            IsDisposed = false,
            HasBeneficiaries = false,
            CreatedAt = DateTime.UtcNow,
            Metadata = new RealEstateMetadata
            {
                Address = "100 Park Ave",
                City = "New York",
                State = "NY",
                ZipCode = "10016",
                PropertyType = PropertyType.Condo,
                SquareFeet = 1200,
                Bedrooms = 2,
                Bathrooms = 1.0
            }
        };

        var result = RoundTrip(original);

        result.Metadata.Should().BeOfType<RealEstateMetadata>()
            .Which.Should().BeEquivalentTo((RealEstateMetadata)original.Metadata!);
    }

    [Fact]
    public void CreateAssetRequest_WithMetadata_RoundTrips()
    {
        var original = new CreateAssetRequest
        {
            Name = "Chase Savings",
            Type = AssetType.BankAccount,
            CurrentValue = 25000d,
            InstitutionName = "Chase",
            Metadata = new BankAccountMetadata
            {
                BankAccountType = BankAccountType.HYSA,
                Apy = 4.5,
                IsJointAccount = true
            }
        };

        var result = RoundTrip(original);

        result.Metadata.Should().BeOfType<BankAccountMetadata>()
            .Which.Should().BeEquivalentTo((BankAccountMetadata)original.Metadata!);
    }

    [Fact]
    public void UpdateAssetRequest_WithMetadata_RoundTrips()
    {
        var original = new UpdateAssetRequest
        {
            Name = "My Bitcoin Wallet",
            Type = AssetType.Cryptocurrency,
            CurrentValue = 100000d,
            Metadata = new CryptocurrencyMetadata
            {
                WalletType = CryptoWalletType.HardwareWallet,
                Holdings =
                [
                    new CryptoHolding
                    {
                        CoinSymbol = "BTC",
                        CoinName = "Bitcoin",
                        Quantity = 1.5,
                        CurrentPrice = 65000d
                    }
                ]
            }
        };

        var result = RoundTrip(original);

        result.Metadata.Should().BeOfType<CryptocurrencyMetadata>()
            .Which.Should().BeEquivalentTo((CryptocurrencyMetadata)original.Metadata!);
    }

    // =========================================================================
    // Union discrimination — all 12 types
    // =========================================================================

    [Theory]
    [MemberData(nameof(AllMetadataTypes))]
    public void AllMetadataTypes_UnionRoundTrip(IAssetMetadata metadata, Type expectedType)
    {
        var result = RoundTripUnion(metadata);

        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    /// <summary>
    ///     Provides one instance of each metadata type for the union round-trip theory.
    /// </summary>
    public static TheoryData<IAssetMetadata, Type> AllMetadataTypes => new()
    {
        { new VehicleMetadata { Make = "Honda", Model = "Civic", Year = 2023 }, typeof(VehicleMetadata) },
        { new RealEstateMetadata { Address = "1 Main St", City = "A", State = "TX", ZipCode = "00000", PropertyType = PropertyType.Land }, typeof(RealEstateMetadata) },
        { new InvestmentMetadata { AccountType = InvestmentAccountType.Individual }, typeof(InvestmentMetadata) },
        { new RetirementMetadata { AccountType = RetirementAccountType.IRA }, typeof(RetirementMetadata) },
        { new BankAccountMetadata { BankAccountType = BankAccountType.Checking }, typeof(BankAccountMetadata) },
        { new InsuranceMetadata { PolicyType = InsurancePolicyType.WholeLife }, typeof(InsuranceMetadata) },
        { new BusinessMetadata { EntityType = BusinessEntityType.LLC, OwnershipPercent = 100 }, typeof(BusinessMetadata) },
        { new PersonalPropertyMetadata { Category = PersonalPropertyCategory.Jewelry }, typeof(PersonalPropertyMetadata) },
        { new CollectibleMetadata { Category = CollectibleCategory.Coins }, typeof(CollectibleMetadata) },
        { new CryptocurrencyMetadata { WalletType = CryptoWalletType.Exchange, Holdings = [new CryptoHolding { CoinSymbol = "BTC", Quantity = 1 }] }, typeof(CryptocurrencyMetadata) },
        { new IntellectualPropertyMetadata { IpType = IpType.Patent }, typeof(IntellectualPropertyMetadata) },
        { new OtherAssetMetadata { Category = "Test" }, typeof(OtherAssetMetadata) }
    };

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    ///     Serializes and deserializes a concrete metadata record.
    /// </summary>
    private static T RoundTrip<T>(T value)
    {
        var bytes = MemoryPackSerializer.Serialize(value);
        bytes.Should().NotBeEmpty("serialization should produce output");

        var result = MemoryPackSerializer.Deserialize<T>(bytes);
        result.Should().NotBeNull("deserialization should succeed");

        return result!;
    }

    /// <summary>
    ///     Serializes and deserializes via the IAssetMetadata union interface.
    /// </summary>
    private static IAssetMetadata RoundTripUnion(IAssetMetadata value)
    {
        var bytes = MemoryPackSerializer.Serialize(value);
        bytes.Should().NotBeEmpty("serialization should produce output");

        var result = MemoryPackSerializer.Deserialize<IAssetMetadata>(bytes);
        result.Should().NotBeNull("union deserialization should succeed");

        return result!;
    }
}
