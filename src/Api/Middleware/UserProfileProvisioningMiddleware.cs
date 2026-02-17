using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SysException = System.Exception;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that ensures an authenticated user has a local
/// <see cref="Shared.Entities.UserProfile"/> record in the database.
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
        catch (OperationCanceledException)
        {
            throw; // Let cancellations propagate
        }
        catch (SysException ex)
        {
            logger.LogWarning(
                ex,
                "UserProfile provisioning failed for request {InvocationId}; continuing pipeline",
                context.InvocationId);
        }

        await next(context);
    }
}
