using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Data;
using RajFinancial.Api.Data.Interceptors;
using RajFinancial.Api.Observability;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Assets;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Tests.Data.Interceptors;

/// <summary>
///     Unit tests for <see cref="BusinessEventsInterceptor"/>. Uses
///     <c>UseInMemoryDatabase</c> with the production interceptor wired in and
///     a <see cref="MeterListener"/> capturing emissions on
///     <see cref="ObservabilityDomains"/> meters.
/// </summary>
public sealed class BusinessEventsInterceptorTests : IDisposable
{
    private readonly MeterListener listener;
    private readonly List<CapturedMeasurement> captured = [];

    // Allow-list of business-event counter instruments emitted by
    // BusinessEventsInterceptor. Filtering on the exact instrument name
    // (instead of just the meter name) keeps these tests immune to other
    // counters or histograms registered on the same meters by parallel tests
    // or by domain telemetry helpers (e.g. the EntitiesQueryDuration
    // histogram or clientmanagement.self_assignment.blocked.count counter).
    private static readonly HashSet<string> BusinessEventInstrumentNames =
    [
        TelemetryMeters.AssetsCreatedInstrument,
        TelemetryMeters.AssetsUpdatedInstrument,
        TelemetryMeters.AssetsDeletedInstrument,
        TelemetryMeters.EntitiesCreatedInstrument,
        TelemetryMeters.EntityRolesAssignedInstrument,
        TelemetryMeters.GrantsCreatedInstrument,
        TelemetryMeters.GrantsRevokedInstrument,
        TelemetryMeters.UserProfileJitProvisionedInstrument,
        TelemetryMeters.UserProfileSyncInstrument,
        TelemetryMeters.UserProfileConcurrentConflictsInstrument,
    ];

    public BusinessEventsInterceptorTests()
    {
        listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (IsBusinessEventInstrument(instrument))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>(OnMeasurement);
        listener.Start();
    }

    public void Dispose() => listener.Dispose();

    [Fact]
    public async Task Adding_an_asset_emits_assets_created_count_with_type_tag()
    {
        await using var db = NewDb(out _);

        db.Assets.Add(NewAsset(AssetType.BankAccount));
        await db.SaveChangesAsync();

        var hit = captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.AssetsCreatedInstrument)
            .Subject;
        hit.Value.Should().Be(1);
        hit.Tags.Should().Contain(new KeyValuePair<string, object?>("asset.type", "BankAccount"));
    }

    [Fact]
    public async Task Modifying_an_asset_with_unchanged_type_emits_type_switch_false()
    {
        await using var db = NewDb(out _);
        var asset = NewAsset(AssetType.BankAccount);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        captured.Clear();

        asset.Name = "Renamed";
        await db.SaveChangesAsync();

        captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.AssetsUpdatedInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("asset.type_switch", false));
    }

    [Fact]
    public async Task Modifying_an_asset_with_changed_type_emits_type_switch_true()
    {
        await using var db = NewDb(out _);
        var asset = NewAsset(AssetType.BankAccount);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        captured.Clear();

        asset.Type = AssetType.Investment;
        await db.SaveChangesAsync();

        var hit = captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.AssetsUpdatedInstrument)
            .Subject;
        hit.Tags.Should().Contain(new KeyValuePair<string, object?>("asset.type_switch", true));
        hit.Tags.Should().Contain(new KeyValuePair<string, object?>("asset.type", "Investment"));
    }

    [Fact]
    public async Task Deleting_an_asset_emits_assets_deleted_count_with_original_type()
    {
        await using var db = NewDb(out _);
        var asset = NewAsset(AssetType.RealEstate);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        captured.Clear();

        db.Assets.Remove(asset);
        await db.SaveChangesAsync();

        captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.AssetsDeletedInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("asset.type", "RealEstate"));
    }

    [Fact]
    public async Task Adding_an_entity_emits_entities_created_count()
    {
        await using var db = NewDb(out _);

        db.Entities.Add(NewEntity());
        await db.SaveChangesAsync();

        captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.EntitiesCreatedInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("entity.type", "Trust"));
    }

    [Fact]
    public async Task Adding_an_entity_role_emits_entity_roles_assigned_count()
    {
        await using var db = NewDb(out _);
        var entity = NewEntity();
        db.Entities.Add(entity);
        await db.SaveChangesAsync();
        captured.Clear();

        db.EntityRoles.Add(new EntityRole
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            ContactId = Guid.NewGuid(),
            RoleType = EntityRoleType.Trustee,
        });
        await db.SaveChangesAsync();

        captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.EntityRolesAssignedInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("entity.role.type", "Trustee"));
    }

    [Fact]
    public async Task Adding_a_data_access_grant_emits_grants_created_count()
    {
        await using var db = NewDb(out _);

        db.DataAccessGrants.Add(NewGrant(GrantStatus.Pending));
        await db.SaveChangesAsync();

        captured.Should().ContainSingle(m => m.Instrument == TelemetryMeters.GrantsCreatedInstrument);
    }

    [Fact]
    public async Task Modifying_grant_status_to_Revoked_emits_grants_revoked_count()
    {
        await using var db = NewDb(out _);
        var grant = NewGrant(GrantStatus.Active);
        db.DataAccessGrants.Add(grant);
        await db.SaveChangesAsync();
        captured.Clear();

        grant.Status = GrantStatus.Revoked;
        await db.SaveChangesAsync();

        captured.Should().ContainSingle(m => m.Instrument == TelemetryMeters.GrantsRevokedInstrument);
    }

    [Fact]
    public async Task Modifying_grant_status_from_Revoked_to_Revoked_emits_no_counter()
    {
        await using var db = NewDb(out _);
        var grant = NewGrant(GrantStatus.Revoked);
        db.DataAccessGrants.Add(grant);
        await db.SaveChangesAsync();
        captured.Clear();

        grant.Notes = "still revoked";
        await db.SaveChangesAsync();

        captured.Should().NotContain(m => m.Instrument == TelemetryMeters.GrantsRevokedInstrument);
    }

    [Fact]
    public async Task Adding_a_user_profile_emits_jit_provisioned_count()
    {
        await using var db = NewDb(out _);

        db.UserProfiles.Add(NewProfile());
        await db.SaveChangesAsync();

        captured.Should().ContainSingle(m => m.Instrument == TelemetryMeters.UserProfileJitProvisionedInstrument);
    }

    [Fact]
    public async Task Modifying_a_user_profile_emits_sync_count()
    {
        await using var db = NewDb(out _);
        var profile = NewProfile();
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        captured.Clear();

        profile.DisplayName = "Renamed";
        await db.SaveChangesAsync();

        captured.Should().ContainSingle(m => m.Instrument == TelemetryMeters.UserProfileSyncInstrument);
    }

    [Fact]
    public async Task JitRace_failed_save_with_added_user_profile_and_DbUpdateException_emits_concurrent_conflicts()
    {
        // Drive the interceptor lifecycle directly so the failed-save path
        // exercises the same code as production without depending on
        // provider-specific failure semantics. The plan requires we run
        // SavingChangesAsync (snapshot) before SaveChangesFailedAsync (emit).
        await using var db = NewSimpleDb(out var interceptor);
        db.UserProfiles.Add(NewProfile());

        await SnapshotAndFail(interceptor, db, new DbUpdateException("jit race"));

        captured.Should().ContainSingle(m =>
            m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("conflict.type", "jit_race"));
        captured.Should().NotContain(m =>
            m.Instrument == TelemetryMeters.UserProfileJitProvisionedInstrument);
    }

    [Fact]
    public async Task ModifyRace_failed_save_with_modified_user_profile_and_concurrency_exception_emits_concurrent_conflicts_once()
    {
        await using var db = NewSimpleDb(out var interceptor);
        var profile = NewProfile();
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        captured.Clear();

        // Mark the existing entity as Modified so the ChangeTracker sees a
        // pending UserProfile update when SavingChangesAsync runs.
        db.Entry(profile).State = EntityState.Modified;

        await SnapshotAndFail(interceptor, db, new DbUpdateConcurrencyException("modify race"));

        var conflicts = captured
            .Where(m => m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument)
            .ToList();
        conflicts.Should().HaveCount(1, because: "subtype-first ordering must not double-emit");
        conflicts[0].Tags.Should()
            .Contain(new KeyValuePair<string, object?>("conflict.type", "modify_race"));
        captured.Should().NotContain(m => m.Instrument == TelemetryMeters.UserProfileSyncInstrument);
    }

    [Fact]
    public async Task Failed_save_with_no_user_profile_in_snapshot_emits_no_counter()
    {
        await using var db = NewSimpleDb(out var interceptor);
        db.Assets.Add(NewAsset(AssetType.BankAccount));

        await SnapshotAndFail(interceptor, db, new DbUpdateException("unrelated failure"));

        captured.Should().NotContain(m =>
            m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument);
        captured.Should().NotContain(m => m.Instrument == TelemetryMeters.AssetsCreatedInstrument);
    }

    [Fact]
    public async Task Failed_save_with_unrelated_exception_emits_no_counter()
    {
        await using var db = NewSimpleDb(out var interceptor);
        db.UserProfiles.Add(NewProfile());

        await SnapshotAndFail(interceptor, db, new InvalidOperationException("boom"));

        captured.Should().BeEmpty();
    }

    // =========================================================================
    // Synchronous SaveChanges coverage — the production runtime is fully async,
    // but the sync overrides on BusinessEventsInterceptor are live code and
    // must hit the same Snapshot / DrainAndEmit / EmitFailureAndClearInternal
    // helpers as their async siblings.
    // =========================================================================

    [Fact]
    public void Sync_SaveChanges_adding_an_asset_emits_assets_created_count()
    {
        using var db = NewDb(out _);

        db.Assets.Add(NewAsset(AssetType.BankAccount));
        db.SaveChanges();

        var hit = captured.Should()
            .ContainSingle(m => m.Instrument == TelemetryMeters.AssetsCreatedInstrument)
            .Subject;
        hit.Value.Should().Be(1);
        hit.Tags.Should().Contain(new KeyValuePair<string, object?>("asset.type", "BankAccount"));
    }

    [Fact]
    public void Sync_SaveChanges_duplicate_user_profile_pk_emits_concurrent_conflicts()
    {
        using var connection = NewSqliteConnection();

        var sharedId = Guid.NewGuid();

        using (var seed = NewSqliteDb(connection))
        {
            seed.UserProfiles.Add(NewProfile(sharedId));
            seed.SaveChanges();
        }

        captured.Clear();

        using var db = NewSqliteDb(connection);
        db.UserProfiles.Add(NewProfile(sharedId));

        var act = () => db.SaveChanges();
        act.Should().Throw<DbUpdateException>();

        captured.Should().ContainSingle(m =>
            m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("conflict.type", "jit_race"));
        captured.Should().NotContain(m =>
            m.Instrument == TelemetryMeters.UserProfileJitProvisionedInstrument);
    }

    // =========================================================================
    // End-to-end EF Core wiring tests — drive a real DbUpdateException through
    // SaveChanges[Async] using the SQLite in-memory provider (which enforces
    // primary-key uniqueness, unlike the InMemory provider) to verify that
    // the registered BusinessEventsInterceptor receives SaveChangesFailed[Async]
    // and emits the expected counter.
    // =========================================================================

    [Fact]
    public async Task EndToEnd_DuplicatePrimaryKey_async_emits_concurrent_conflicts()
    {
        await using var connection = NewSqliteConnection();

        var sharedId = Guid.NewGuid();

        await using (var seed = NewSqliteDb(connection))
        {
            seed.UserProfiles.Add(NewProfile(sharedId));
            await seed.SaveChangesAsync();
        }

        captured.Clear();

        await using var db = NewSqliteDb(connection);
        db.UserProfiles.Add(NewProfile(sharedId));

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();

        captured.Should().ContainSingle(m =>
            m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("conflict.type", "jit_race"));
        captured.Should().NotContain(m =>
            m.Instrument == TelemetryMeters.UserProfileJitProvisionedInstrument);
    }

    [Fact]
    public void EndToEnd_DuplicatePrimaryKey_sync_emits_concurrent_conflicts()
    {
        using var connection = NewSqliteConnection();

        var sharedId = Guid.NewGuid();

        using (var seed = NewSqliteDb(connection))
        {
            seed.UserProfiles.Add(NewProfile(sharedId));
            seed.SaveChanges();
        }

        captured.Clear();

        using var db = NewSqliteDb(connection);
        db.UserProfiles.Add(NewProfile(sharedId));

        var act = () => db.SaveChanges();
        act.Should().Throw<DbUpdateException>();

        captured.Should().ContainSingle(m =>
            m.Instrument == TelemetryMeters.UserProfileConcurrentConflictsInstrument)
            .Which.Tags.Should()
            .Contain(new KeyValuePair<string, object?>("conflict.type", "jit_race"));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void OnMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        captured.Add(new CapturedMeasurement(instrument.Name, measurement, tags.ToArray()));
    }

    private static bool IsBusinessEventInstrument(Instrument instrument) =>
        IsDomainMeter(instrument.Meter.Name)
        && BusinessEventInstrumentNames.Contains(instrument.Name);

    private static bool IsDomainMeter(string meterName) =>
        meterName is ObservabilityDomains.Assets
            or ObservabilityDomains.Entities
            or ObservabilityDomains.ClientManagement
            or ObservabilityDomains.UserProfile;

    private static ApplicationDbContext NewDb(out string connectionName)
    {
        connectionName = Guid.NewGuid().ToString();
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(connectionName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new BusinessEventsInterceptor());

        return new ApplicationDbContext(builder.Options);
    }

    /// <summary>
    ///     Builds a DbContext that uses a directly accessible
    ///     <see cref="BusinessEventsInterceptor"/> instance, so failed-save
    ///     paths can be exercised by driving the interceptor lifecycle directly
    ///     without depending on EF's catch ordering when an interceptor throws
    ///     in <c>SavingChangesAsync</c>.
    /// </summary>
    private static ApplicationDbContext NewSimpleDb(out BusinessEventsInterceptor interceptor)
    {
        interceptor = new BusinessEventsInterceptor();
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptor);
        return new ApplicationDbContext(builder.Options);
    }

    private static SqliteConnection NewSqliteConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // Hand-rolled UserProfiles schema. EnsureCreated() can't be used here
        // because the production model declares "nvarchar(max)" column types
        // that are SQL Server-specific. The test only needs a single table
        // with PK enforcement to drive a real DbUpdateException through the
        // interceptor pipeline.
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE UserProfiles (
                Id TEXT NOT NULL PRIMARY KEY,
                Email TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Role TEXT NOT NULL,
                TenantId TEXT NOT NULL,
                PhoneNumber TEXT NULL,
                IsProfileComplete INTEGER NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NULL,
                LastLoginAt TEXT NULL,
                PreferencesJson TEXT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        return connection;
    }

    private static ApplicationDbContext NewSqliteDb(SqliteConnection connection)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new BusinessEventsInterceptor());
        return new ApplicationDbContext(builder.Options);
    }

    /// <summary>
    ///     Drives the snapshot → fail lifecycle directly via internal test
    ///     hooks. The unit tests in this file exercise the
    ///     <c>SaveChangesFailedAsync</c> handler logic in isolation;
    ///     production failure modes (DB rejection on duplicate PK, optimistic
    ///     concurrency token mismatch) reliably trigger
    ///     <c>SaveChangesFailedAsync</c>, and that EF Core wiring is verified
    ///     end-to-end by the SQLite-backed tests below
    ///     (<see cref="EndToEnd_DuplicatePrimaryKey_async_emits_concurrent_conflicts"/>
    ///     and <see cref="EndToEnd_DuplicatePrimaryKey_sync_emits_concurrent_conflicts"/>).
    /// </summary>
    private static Task SnapshotAndFail(
        BusinessEventsInterceptor interceptor,
        DbContext context,
        Exception exception)
    {
        interceptor.SnapshotForTesting(context);
        interceptor.EmitFailureAndClearForTesting(exception);
        return Task.CompletedTask;
    }

    private static Asset NewAsset(AssetType type) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Test Asset",
        Type = type,
        CurrentValue = 100m,
    };

    private static Entity NewEntity() => new()
    {
        Id = Guid.NewGuid(),
        Type = EntityType.Trust,
        Name = "Test Trust",
        Slug = $"trust-{Guid.NewGuid():N}",
    };

    private static DataAccessGrant NewGrant(GrantStatus status) => new()
    {
        Id = Guid.NewGuid(),
        GrantorUserId = Guid.NewGuid(),
        GranteeEmail = "test@example.com",
        AccessType = AccessType.Read,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static UserProfile NewProfile(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "test@example.com",
        DisplayName = "Test User",
        TenantId = Guid.NewGuid(),
    };

    private sealed record CapturedMeasurement(
        string Instrument,
        long Value,
        IReadOnlyList<KeyValuePair<string, object?>> Tags);
}
