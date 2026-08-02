using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class AnnouncementService : CrudService<int, CreateAnnouncementModel, AnnouncementModel, Announcement>, IAnnouncementService
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IValidator<CreateAnnouncementModel>? _createValidator;

    public AnnouncementService(
        ICrudServiceDependencyAggregate<CreateAnnouncementModel, AnnouncementModel> dependencyAggregate,
        IApplicationDbContext appDbContext,
        IValidator<CreateAnnouncementModel>? createValidator = null
    ) : base(dependencyAggregate)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
        _createValidator = createValidator;
    }

    /// <inheritdoc />
    public async Task<Result<AnnouncementModel>> CreateAsync(CreateAnnouncementModel model, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<AnnouncementModel>(errors);
            }
        }

        var announcement = new Announcement
        {
            Content = model.Content.Trim(),
            ShowOnLoginPage = model.ShowOnLoginPage,
            ExpiryDateTimeUtc = model.ExpiryDateTimeUtc,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserID = createdByUserId,
        };

        _appDbContext.Announcement.Add(announcement);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(ToModel(announcement));
    }

    /// <inheritdoc />
    public async Task<List<ActiveAnnouncementModel>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _appDbContext.Announcement
            .Where(a => a.ExpiryDateTimeUtc == null || a.ExpiryDateTimeUtc > now)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new ActiveAnnouncementModel
            {
                AnnouncementID = a.AnnouncementID,
                Content = a.Content,
                ShowOnLoginPage = a.ShowOnLoginPage,
                CreatedAtUtc = a.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<AnnouncementModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var announcements = await _appDbContext.Announcement
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return announcements.Select(ToModel).ToList();
    }

    private static AnnouncementModel ToModel(Announcement announcement) => new()
    {
        AnnouncementID = announcement.AnnouncementID,
        Content = announcement.Content,
        ShowOnLoginPage = announcement.ShowOnLoginPage,
        ExpiryDateTimeUtc = announcement.ExpiryDateTimeUtc,
        CreatedAtUtc = announcement.CreatedAtUtc,
        CreatedByUserID = announcement.CreatedByUserID,
    };
}
