using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace RajFinancial.Client;

/// <summary>
/// Custom account factory that properly parses the 'roles' claim from Entra ID tokens.
/// Entra ID returns roles as a JSON array, but .NET's IsInRole() expects individual role claims.
/// This factory converts the array into separate ClaimTypes.Role claims.
/// </summary>
public class CustomAccountFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    /// <summary>
    /// Initializes a new instance of the CustomAccountFactory.
    /// </summary>
    public CustomAccountFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor)
    {
    }

    /// <summary>
    /// Creates a ClaimsPrincipal from the remote user account,
    /// properly parsing array-based role claims from Entra ID.
    /// </summary>
    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is ClaimsIdentity identity && account?.AdditionalProperties.TryGetValue("roles", out var rolesObj) == true)
        {
            // Remove any existing role claims to avoid duplicates
            var existingRolesClaims = identity.FindAll(ClaimTypes.Role).ToList();
            foreach (var claim in existingRolesClaims)
            {
                identity.RemoveClaim(claim);
            }

            // Parse roles from the JSON element
            if (rolesObj is JsonElement rolesElement)
            {
                AddRolesFromJsonElement(identity, rolesElement);
            }
        }

        return user;
    }

    /// <summary>
    /// Extracts roles from a JsonElement and adds them as individual role claims.
    /// </summary>
    private static void AddRolesFromJsonElement(ClaimsIdentity identity, JsonElement rolesElement)
    {
        switch (rolesElement.ValueKind)
        {
            case JsonValueKind.Array:
                // Multiple roles - add each as a separate role claim
                foreach (var role in rolesElement.EnumerateArray())
                {
                    AddRoleClaim(identity, role.GetString());
                }
                break;

            case JsonValueKind.String:
                // Single role as string
                AddRoleClaim(identity, rolesElement.GetString());
                break;
        }
    }

    /// <summary>
    /// Adds a role claim if the value is not null or empty.
    /// </summary>
    private static void AddRoleClaim(ClaimsIdentity identity, string? roleValue)
    {
        if (!string.IsNullOrWhiteSpace(roleValue))
        {
            // Add as standard .NET role claim type (required for IsInRole() to work)
            identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
            
            // Also keep a copy with 'roles' type for debugging/display purposes
            identity.AddClaim(new Claim("roles", roleValue));
        }
    }
}
