using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Services.EntityService;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Shared.Entities.Users;
using SysException = System.Exception;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that ensures an authenticated user has a local
/// <see cref="UserProfile"/> record in the database.
/// </summary>
/// <remarks>
/// <para>
/// <b>JIT (Just-In-Time) Provisioning:</b> On the first authenticated request a
/// new <c>UserProfile</c> row is inserted. On subsequent requests the mutable
/// claim data (email, display name, role) is synced and <c>LastLoginAt</c> is
/// stamped.
/// </para>
/// <para>
/// This middleware must execute <b>after</b> <see cref="AuthenticationMiddleware"/>
/// (so claim-based context items are available) and <b>before</b>
/// <see cref="Authorization.AuthorizationMiddleware"/> (so authorization can rely on the
/// persisted profile).
/// </para>
/// <para>
/// Provisioning failures are swallowed and logged as warnings so they never block
/// the request pipeline.
/// </para>
/// </remarks>
public partial class UserProfileProvisioningMiddleware(
    ILogger<UserProfileProvisioningMiddleware> logger) : IFunctionsWorkerMiddleware
{
    /// <inheritdoc/>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            if (context.IsAuthenticated())
            {
                var userIdGuid = context.GetUserIdAsGuid();

                if (userIdGuid.HasValue)
                {
                    var email = context.GetUserEmail() ?? string.Empty;
                    var displayName = context.GetUserName();
                    var roles = context.GetUserRoles();

                    var profileService = context.InstanceServices
                        .GetRequiredService<IUserProfileService>();

                    await profileService.EnsureProfileExistsAsync(
                        userIdGuid.Value,
                        email,
                        displayName,
                        roles);

                    // Auto-provision the user's Personal entity.
                    // Always called (not just for "new" profiles) so the system self-heals
                    // if the Personal entity is ever missing. The service is idempotent —
                    // a single indexed lookup on (UserId, Type=Personal) and returns fast
                    // when the entity already exists.
                    var entityService = context.InstanceServices
                        .GetRequiredService<IEntityService>();

                    await entityService.EnsurePersonalEntityAsync(userIdGuid.Value);
                }
            }
        }
        catch (SysException ex)
        {
            if (IsCriticalException(ex))
            {
                throw;
            }

            LogProvisioningFailed(ex, context.InvocationId);
        }

        await next(context);
    }

    [LoggerMessage(EventId = 5100, Level = LogLevel.Warning,
        Message = "UserProfile provisioning failed for request {InvocationId}; continuing pipeline")]
    private partial void LogProvisioningFailed(SysException ex, string invocationId);

    /// <summary>
    /// Returns <c>true</c> for fatal/system exceptions that must never be swallowed.
    /// </summary>
    private static bool IsCriticalException(SysException ex)
    {
        return ex is OutOfMemoryException
            or StackOverflowException
            or OperationCanceledException
            or ThreadAbortException
            or CannotUnloadAppDomainException
            or ThreadInterruptedException
            or ThreadStartException;
    }
}
