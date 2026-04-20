using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities.Assets;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Asset"/> entity (TPH base).
/// Defines TPH discriminator, indexes, property constraints, decimal precision,
/// and relationship to <see cref="UserProfile"/>.
/// </summary>
public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(e => e.Id);

        // TPH discriminator
        builder.HasDiscriminator<string>("AssetDiscriminator")
            .HasValue<Asset>("Asset")
            .HasValue<DepreciableAsset>("DepreciableAsset");

        // Indexes for efficient queries
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Type });
        builder.HasIndex(e => new { e.UserId, e.IsDisposed });

        // Property constraints
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Location).HasMaxLength(500);
        builder.Property(e => e.AccountNumber).HasMaxLength(100);
        builder.Property(e => e.InstitutionName).HasMaxLength(200);
        builder.Property(e => e.DisposalNotes).HasMaxLength(2000);

        // Decimal precision
        builder.Property(e => e.CurrentValue).HasPrecision(18, 2);
        builder.Property(e => e.PurchasePrice).HasPrecision(18, 2);
        builder.Property(e => e.DisposalPrice).HasPrecision(18, 2);
        builder.Property(e => e.MarketValue).HasPrecision(18, 2);

        // Store enum as string for readability
        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        // FK to UserProfile with restrict delete
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
