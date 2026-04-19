using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
/// <see cref="AuthorizationMiddleware"/> (so authorization can rely on the
/// persisted profile).
/// </para>
/// <para>
/// Provisioning failures are swallowed and logged as warnings so they never block
/// the request pipeline.
/// </para>
/// </remarks>
public class UserProfileProvisioningMiddleware(
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
                        .GetRequiredService<Services.UserProfiles.IUserProfileService>();

                    await profileService.EnsureProfileExistsAsync(
                        userIdGuid.Value,
                        email,
                        displayName,
                        roles);
                }
            }
        }
        catch (SysException ex)
        {
            if (IsCriticalException(ex))
            {
                throw;
            }

            logger.LogWarning(
                ex,
                "UserProfile provisioning failed for request {InvocationId}; continuing pipeline",
                context.InvocationId);
        }

        await next(context);
    }

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
