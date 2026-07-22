namespace Predictathon.Application.Models;

/// <summary>
/// The full set of roles a user should hold after the update - not a delta. The service diffs
/// this against the user's current roles to work out what to add/remove.
/// </summary>
public class UpdateUserRolesModel
{
    public string[] Roles { get; set; } = [];
}
