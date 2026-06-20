using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Infrastructure.Persistence;

public partial class ApplicationDbContext : GenericDbContext<ApplicationDbContext>, IApplicationDbContext { }