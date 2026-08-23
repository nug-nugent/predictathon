using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.Application.Extensions;

/// <summary>
/// Helpers shared by the services that return <see cref="LeagueTableItem"/> rows - a single
/// competition's table (LeagueTableGet) and the all-time table (Statistics_AllTimeLeagueTableGet).
/// </summary>
public static class LeagueTableItemExtensions
{
    /// <summary>
    /// Fills in each row's <see cref="LeagueTableItem.AvatarUrl"/> from the ImageUploaded flag the
    /// stored procedure returned, so clients can show a player's avatar beside their entry without
    /// a per-user lookup.
    /// </summary>
    /// <param name="items">The league table rows to populate.</param>
    /// <param name="avatarService">The service that resolves a user's avatar URL.</param>
    /// <returns>The same rows, for convenient chaining onto the procedure call.</returns>
    public static IReadOnlyList<LeagueTableItem> WithAvatarUrls(this IReadOnlyList<LeagueTableItem> items, IAvatarService avatarService)
    {
        foreach (var item in items)
        {
            item.AvatarUrl = avatarService.GetAvatarUrl(item.UserID, item.ImageUploaded);
        }

        return items;
    }
}
