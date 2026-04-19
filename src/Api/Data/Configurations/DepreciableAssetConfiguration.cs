using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="DepreciableAsset"/> entity (TPH derived).
/// Defines decimal precision and enum conversion for depreciation-specific properties.
/// </summary>
public class DepreciableAssetConfiguration : IEntityTypeConfiguration<DepreciableAsset>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DepreciableAsset> builder)
    {
        builder.Property(e => e.SalvageValue).HasPrecision(18, 2);

        // Store enum as string for readability
        builder.Property(e => e.DepreciationMethod)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
