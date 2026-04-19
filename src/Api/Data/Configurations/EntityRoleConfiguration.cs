using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
///     EF Core configuration for the <see cref="EntityRole"/> class.
/// </summary>
public class EntityRoleConfiguration : IEntityTypeConfiguration<EntityRole>
{
    public void Configure(EntityTypeBuilder<EntityRole> builder)
    {
        builder.ToTable("EntityRoles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.OwnershipPercent).HasPrecision(5, 2);
        builder.Property(e => e.BeneficialInterestPercent).HasPrecision(5, 2);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasIndex(e => e.EntityId);
        builder.HasIndex(e => e.ContactId);

        builder.HasOne(e => e.Entity)
            .WithMany(e => e.Roles)
            .HasForeignKey(e => e.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
