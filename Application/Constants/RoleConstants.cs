namespace Predictathon.Application.Constants;

/// <summary>
/// Identity role names, centralized so [Authorize(Roles = ...)] attributes and role-seeding code
/// reference the same constants rather than repeated string literals.
/// </summary>
public static class RoleConstants
{
    public const string MatchAdministrator = "MatchAdministrator";

    public const string UserAdministrator = "UserAdministrator";

    public const string CompetitionAdministrator = "CompetitionAdministrator";
}
