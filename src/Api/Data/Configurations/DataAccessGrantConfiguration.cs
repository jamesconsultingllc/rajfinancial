using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="DataAccessGrant"/> entity.
/// Defines indexes, property constraints, enum conversions, JSON serialization
/// for Categories, and relationships to <see cref="UserProfile"/>.
/// </summary>
public class DataAccessGrantConfiguration : IEntityTypeConfiguration<DataAccessGrant>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DataAccessGrant> builder)
    {
        builder.HasKey(e => e.Id);

        // Indexes for efficient queries
        builder.HasIndex(e => new { e.GrantorUserId, e.GranteeUserId });
        builder.HasIndex(e => e.GranteeUserId);
        builder.HasIndex(e => e.GranteeEmail);
        builder.HasIndex(e => e.InvitationToken).IsUnique();
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.GrantorUserId).IsRequired();
        builder.Property(e => e.GranteeEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.RelationshipLabel).HasMaxLength(100);
        builder.Property(e => e.InvitationToken).HasMaxLength(128);
        builder.Property(e => e.Notes).HasMaxLength(500);

        // Store enums as strings for readability
        builder.Property(e => e.AccessType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Store categories as JSON with proper value comparer
        builder.Property(e => e.Categories)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
                c => c.Aggregate(
                    0,
                    (hash, item) => HashCode.Combine(
                        hash,
                        StringComparer.Ordinal.GetHashCode(item))),
                c => c.ToList()));

        // Configure relationships
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.GrantorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.GranteeUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
