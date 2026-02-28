using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Polymorphic interface for per-type asset metadata.
///     Uses MemoryPack union tags for discriminated serialization.
/// </summary>
/// <remarks>
///     Each asset type has a corresponding metadata record implementing this interface.
///     The union tag determines which concrete type to deserialize at runtime.
/// </remarks>
[MemoryPackable]
[MemoryPackUnion(0, typeof(VehicleMetadata))]
[MemoryPackUnion(1, typeof(RealEstateMetadata))]
[MemoryPackUnion(2, typeof(InvestmentMetadata))]
[MemoryPackUnion(3, typeof(RetirementMetadata))]
[MemoryPackUnion(4, typeof(BankAccountMetadata))]
[MemoryPackUnion(5, typeof(InsuranceMetadata))]
[MemoryPackUnion(6, typeof(BusinessMetadata))]
[MemoryPackUnion(7, typeof(PersonalPropertyMetadata))]
[MemoryPackUnion(8, typeof(CollectibleMetadata))]
[MemoryPackUnion(9, typeof(CryptocurrencyMetadata))]
[MemoryPackUnion(10, typeof(IntellectualPropertyMetadata))]
[MemoryPackUnion(11, typeof(OtherAssetMetadata))]
[GenerateTypeScript]
public partial interface IAssetMetadata;
