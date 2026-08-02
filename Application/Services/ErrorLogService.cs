using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Services;

[ScopedService]
public class ErrorLogService : IErrorLogService
{
    private readonly IApplicationDbContext _dbContext;

    public ErrorLogService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ErrorLogListItem>> GetErrorsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ErrorLog.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(e => e.TimeStampUtc)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(e => new ErrorLogListItem
        {
            Id = e.Id,
            Level = e.Level,
            Message = e.Message,
            // The sink stores UTC but the column round-trips with an unspecified kind - restamp it
            // so System.Text.Json serialises with a Z suffix and browsers convert to local time.
            TimeStampUtc = DateTime.SpecifyKind(e.TimeStampUtc, DateTimeKind.Utc),
            Exception = e.Exception,
            Properties = e.Properties,
        }).ToList();

        return new PagedResult<ErrorLogListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
