namespace RajFinancial.Api.Configuration;

/// <summary>
/// Configuration options for Entra External ID app role mappings.
/// These role GUIDs differ between development and production environments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authorization Model: Implicit Client + Explicit Administrator</b>
/// </para>
/// <para>
/// The application uses a simplified two-role model:
/// <list type="bullet">
///   <item><c>Client</c> - IMPLICIT for all authenticated users. No assignment required.</item>
///   <item><c>Administrator</c> - EXPLICIT assignment required via Azure Portal.</item>
/// </list>
/// </para>
/// <para>
/// <b>Why Client Role Exists But Isn't Assigned:</b>
/// The Client role is defined in Entra ID but users don't need it explicitly assigned.
/// All authenticated users without a role are treated as Clients automatically.
/// This avoids the need for API Connectors or Entra ID P1/P2 dynamic groups.
/// The GUID is kept for:
/// <list type="bullet">
///   <item>Optional explicit assignment if needed in the future</item>
///   <item>Token validation when a user HAS the role claim</item>
///   <item>Consistency with Entra ID app manifest</item>
/// </list>
/// </para>
/// <para>
/// Fine-grained data access (e.g., for spouses, attorneys, CPAs viewing client data)
/// is handled through <c>DataAccessGrant</c> entities, not through app roles.
/// </para>
/// <para>
/// <b>Extension Strategy:</b>
/// To add new roles (e.g., Professional for CPA firms):
/// <list type="number">
///   <item>Add new property here (e.g., <c>Professional</c>)</item>
///   <item>Update <c>GetRoleId</c> switch expression</item>
///   <item>Add role GUID to appsettings.json and local.settings.json</item>
///   <item>Register role in Entra ID app registration</item>
///   <item>Update authorization policies in Client/Program.cs</item>
/// </list>
/// </para>
/// </remarks>
public class AppRoleOptions
{
    /// <summary>
    /// Configuration section name in appsettings/local.settings.json.
    /// </summary>
    public const string SectionName = "AppRoles";

    /// <summary>
    /// Gets or sets the GUID for the Client role.
    /// </summary>
    /// <remarks>
    /// This role is IMPLICIT - all authenticated users are treated as Clients
    /// even without explicit role assignment. The GUID is kept for:
    /// <list type="bullet">
    ///   <item>Future explicit assignment if needed</item>
    ///   <item>Role validation in tokens when explicitly assigned</item>
    ///   <item>Consistency with Entra ID app registration</item>
    /// </list>
    /// </remarks>
    public Guid Client { get; set; }

    /// <summary>
    /// Gets or sets the GUID for the Administrator role.
    /// </summary>
    /// <remarks>
    /// This role REQUIRES explicit assignment in Azure Portal.
    /// Administrators have access to platform management features.
    /// </remarks>
    public Guid Administrator { get; set; }

    // =========================================================================
    // FUTURE EXTENSION: Uncomment when professional/organization roles are needed
    // =========================================================================
    // /// <summary>
    // /// Gets or sets the GUID for the Professional role.
    // /// Users who manage data on behalf of clients (CPAs, attorneys, advisors).
    // /// </summary>
    // public Guid Professional { get; set; }
    //
    // /// <summary>
    // /// Gets or sets the GUID for the OrganizationAdmin role.
    // /// Administrators of a firm/practice who can manage their organization's users.
    // /// </summary>
    // public Guid OrganizationAdmin { get; set; }
    // =========================================================================

    /// <summary>
    /// Gets the role GUID by role name.
    /// </summary>
    /// <param name="roleName">The name of the role (case-insensitive).</param>
    /// <returns>The GUID for the role, or null if not found.</returns>
    public Guid? GetRoleId(string roleName)
    {
        return roleName.ToUpperInvariant() switch
        {
            "CLIENT" => Client,
            "ADMINISTRATOR" => Administrator,
            // FUTURE: Add new roles here
            // "PROFESSIONAL" => Professional,
            // "ORGANIZATIONADMIN" => OrganizationAdmin,
            _ => null
        };
    }

    /// <summary>
    /// Checks if a role name is valid.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>True if the role exists, false otherwise.</returns>
    public bool IsValidRole(string roleName)
    {
        return GetRoleId(roleName) != null;
    }
}
