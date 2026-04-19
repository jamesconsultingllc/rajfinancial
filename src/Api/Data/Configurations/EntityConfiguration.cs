using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
///     EF Core configuration for the <see cref="Entity"/> class.
///     Configures JSON metadata columns, indexes, and relationships.
/// </summary>
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("Entities");
        builder.HasKey(e => e.Id);

        // Unique slug per user
        builder.HasIndex(e => new { e.UserId, e.Slug }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Type });

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

        // Type-specific metadata stored as JSON columns.
        // Nested collections of complex types inside JSON-owned entities
        // must be declared with OwnsMany so EF treats them as part of the JSON document.
        builder.OwnsOne(e => e.Business, b =>
        {
            b.ToJson();
            b.OwnsMany(x => x.Registrations);
        });
        builder.OwnsOne(e => e.Trust, t => t.ToJson());

        // Self-referencing parent-child
        builder.HasOne(e => e.ParentEntity)
            .WithMany(e => e.ChildEntities)
            .HasForeignKey(e => e.ParentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to UserProfile (owner)
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
