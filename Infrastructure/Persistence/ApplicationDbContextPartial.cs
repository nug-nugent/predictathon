using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Infrastructure.Persistence;

public partial class ApplicationDbContext : IApplicationDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureIdentity(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
    }
}