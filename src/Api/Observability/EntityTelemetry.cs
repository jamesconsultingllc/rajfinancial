using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Entities OTel instruments shared by
///     <see cref="RajFinancial.Api.Functions.Entities.EntityFunctions" /> and
///     <see cref="RajFinancial.Api.Services.EntityService.EntityService" />.
///     Business counters (entities.created.count / entities.roles.assigned.count)
///     are owned by <see cref="RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor"/>
///     and exposed via <see cref="TelemetryMeters"/>. Only the EntitiesQueryDuration
///     histogram lives here — it measures request-path query time, not save events.
/// </summary>
internal static class EntityTelemetry
{
    internal const string SourceName = ObservabilityDomains.Entities;

    private const string EntitiesQueryDurationInstrument = "entities.query.duration.ms";

    // Tag keys.
    internal const string UserIdTag = "user.id";
    internal const string EntityIdTag = "entity.id";
    internal const string EntityTypeTag = "entity.type";
    internal const string EntitySlugTag = "entity.slug";
    internal const string EntityOwnerIdTag = "entity.owner.id";
    internal const string EntityRoleIdTag = "entity.role.id";
    internal const string EntityRoleTypeTag = "entity.role.type";
    internal const string EntitiesQueryOpTag = "entities.query.op";

    // Activity (span) names — endpoint spans are emitted by the HTTP function, service spans by the
    // domain service. Keeping distinct names makes nested traces easier to read.
    internal const string ActivityGetEntities = "Entities.GetEntities";
    internal const string ActivityGetEntityById = "Entities.GetEntityById";
    internal const string ActivityCreateEntity = "Entities.CreateEntity";
    internal const string ActivityUpdateEntity = "Entities.UpdateEntity";
    internal const string ActivityDeleteEntity = "Entities.DeleteEntity";
    internal const string ActivityGetEntityRoles = "Entities.GetEntityRoles";
    internal const string ActivityAssignEntityRole = "Entities.AssignEntityRole";
    internal const string ActivityRemoveEntityRole = "Entities.RemoveEntityRole";

    internal const string ActivityGetEntitiesService = "Entities.GetEntities.Service";
    internal const string ActivityGetEntityByIdService = "Entities.GetEntityById.Service";
    internal const string ActivityCreateEntityService = "Entities.CreateEntity.Service";
    internal const string ActivityUpdateEntityService = "Entities.UpdateEntity.Service";
    internal const string ActivityDeleteEntityService = "Entities.DeleteEntity.Service";
    internal const string ActivityGetRolesService = "Entities.GetRoles.Service";
    internal const string ActivityAssignRoleService = "Entities.AssignRole.Service";
    internal const string ActivityRemoveRoleService = "Entities.RemoveRole.Service";
    internal const string ActivityEnsurePersonalEntityService = "Entities.EnsurePersonalEntity.Service";

    // Query op tags for the histogram.
    internal const string QueryOpGetEntities = "get_entities";
    internal const string QueryOpGetEntityById = "get_entity_by_id";
    internal const string QueryOpGetRoles = "get_roles";

    private static readonly ActivitySource ActivitySource = new(SourceName);

    // Reuse the shared Meter instance so we never end up with two Meter
    // objects sharing a name (which would double-emit).
    private static readonly Histogram<double> EntitiesQueryDuration =
        TelemetryMeters.EntitiesMeter.CreateHistogram<double>(EntitiesQueryDurationInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordQueryDuration(string queryOp, double elapsedMs)
    {
        var tags = new TagList { { EntitiesQueryOpTag, queryOp } };
        EntitiesQueryDuration.Record(elapsedMs, tags);
    }
}
