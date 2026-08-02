using FluentResults;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

// TCreateModel: CreateAnnouncementModel (client-supplied fields)
// TEditModel: AnnouncementModel (full model including AnnouncementID and audit fields)
public interface IAnnouncementService : ICrudService<int, CreateAnnouncementModel, AnnouncementModel, Announcement>
{
    /// <summary>
    /// Creates a new announcement, recording who posted it. Used instead of the inherited
    /// <see cref="ICrudService{TPrimaryKey, TCreateModel, TEditModel, TEntity}.Create"/> because
    /// <see cref="CreateAnnouncementModel"/> has no field for the posting admin's id - the caller
    /// (the authenticated controller action) supplies it explicitly.
    /// </summary>
    Task<Result<AnnouncementModel>> CreateAsync(CreateAnnouncementModel model, Guid createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every non-expired announcement, most recent first, for the public homepage/login-page feed.
    /// </summary>
    Task<List<ActiveAnnouncementModel>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every announcement including expired ones, most recent first, for the Announcements Admin page.
    /// </summary>
    Task<List<AnnouncementModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
