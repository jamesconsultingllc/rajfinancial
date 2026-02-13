using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data;

/// <summary>
/// Main database context for RAJ Financial application.
/// Configured to use Azure SQL with Managed Identity authentication.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// User profiles linked to Entra External ID.
    /// </summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    /// <summary>
    /// Data access grants between users (owner grants access to others).
    /// </summary>
    public DbSet<DataAccessGrant> DataAccessGrants => Set<DataAccessGrant>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index for fast lookups by email and tenant
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();

            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PreferencesJson).HasColumnType("nvarchar(max)");

            // Store enum as string for readability
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // Configure DataAccessGrant
        modelBuilder.Entity<DataAccessGrant>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes for efficient queries
            entity.HasIndex(e => new { e.GrantorUserId, e.GranteeUserId });
            entity.HasIndex(e => e.GranteeUserId);
            entity.HasIndex(e => e.GranteeEmail);
            entity.HasIndex(e => e.InvitationToken).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.GrantorUserId).IsRequired();
            entity.Property(e => e.GranteeEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.RelationshipLabel).HasMaxLength(100);
            entity.Property(e => e.InvitationToken).HasMaxLength(128);
            entity.Property(e => e.Notes).HasMaxLength(500);

            // Store enum as string for readability
            entity.Property(e => e.AccessType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Store categories as JSON
            entity.Property(e => e.Categories)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            // Configure relationships
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.GrantorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.GranteeUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
