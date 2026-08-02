using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IErrorLogService
{
    /// <summary>
    /// Gets a server-paged list of logged errors/warnings, newest first, for the admin Error Log page.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of rows per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedResult<ErrorLogListItem>> GetErrorsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
