namespace Predictathon.Application.Models;

/// <summary>
/// A single row of the User Admin list - enough to display and manage a user's roles/lockout
/// state without loading their full profile.
/// </summary>
public class UserAdminListItem
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Forenames { get; set; }

    public string? Surname { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public bool IsLockedOut { get; set; }

    public DateTime? LastLoginDateTime { get; set; }
}
