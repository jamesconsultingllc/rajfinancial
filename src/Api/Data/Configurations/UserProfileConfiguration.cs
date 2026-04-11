using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="UserProfile"/> entity.
/// Defines indexes, property constraints, and enum conversions.
/// </summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(e => e.Id);

        // TenantId index for tenant-scoped queries (not unique — UserId is the PK)
        builder.HasIndex(e => e.TenantId);

        // Email is optional — populated from Entra claims when available
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.DisplayName).HasMaxLength(256);
        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.PreferencesJson).HasColumnType("nvarchar(max)");
    }
}
