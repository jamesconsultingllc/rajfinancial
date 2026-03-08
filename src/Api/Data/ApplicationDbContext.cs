using Microsoft.EntityFrameworkCore;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data;

/// <summary>
/// Main database context for RAJ Financial application.
/// Configured to use Azure SQL with Managed Identity authentication.
/// Entity configurations are in <c>Data/Configurations/</c>.
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

    /// <summary>
    /// Assets owned by users (TPH base: includes both Asset and DepreciableAsset).
    /// </summary>
    public DbSet<Asset> Assets => Set<Asset>();

    /// <summary>
    /// Depreciable assets (TPH derived: physical assets that lose value over time).
    /// </summary>
    public DbSet<DepreciableAsset> DepreciableAssets => Set<DepreciableAsset>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
