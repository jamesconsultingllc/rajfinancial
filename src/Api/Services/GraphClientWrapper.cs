using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.AppRoleAssignments;

namespace RajFinancial.Api.Services;

/// <summary>
/// Concrete implementation of IGraphClientWrapper using Microsoft Graph SDK.
/// </summary>
public class GraphClientWrapper : IGraphClientWrapper
{
    private readonly GraphServiceClient _graphClient;

    public GraphClientWrapper(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    /// <inheritdoc />
    public async Task<AppRoleAssignmentCollectionResponse?> GetUserAppRoleAssignmentsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _graphClient.Users[userId]
            .AppRoleAssignments
            .GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AppRoleAssignment?> AssignAppRoleToUserAsync(
        string userId,
        AppRoleAssignment appRoleAssignment,
        CancellationToken cancellationToken = default)
    {
        return await _graphClient.Users[userId]
            .AppRoleAssignments
            .PostAsync(appRoleAssignment, cancellationToken: cancellationToken);
    }
}
