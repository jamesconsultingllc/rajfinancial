using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.AppRoleAssignments;

namespace RajFinancial.Api.Services;

/// <summary>
/// Interface wrapper for Microsoft Graph API operations.
/// Enables testability by providing a mockable abstraction over GraphServiceClient.
/// </summary>
public interface IGraphClientWrapper
{
    /// <summary>
    /// Gets the app role assignments for a specified user.
    /// </summary>
    /// <param name="userId">The user's object ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of app role assignments.</returns>
    Task<AppRoleAssignmentCollectionResponse?> GetUserAppRoleAssignmentsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns an app role to a user.
    /// </summary>
    /// <param name="userId">The user's object ID.</param>
    /// <param name="appRoleAssignment">The app role assignment to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created app role assignment.</returns>
    Task<AppRoleAssignment?> AssignAppRoleToUserAsync(
        string userId,
        AppRoleAssignment appRoleAssignment,
        CancellationToken cancellationToken = default);
}
