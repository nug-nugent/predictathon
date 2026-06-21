using MapsterMapper;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Application.Services.Persistence;

[ScopedService]
public class GenericDbContextDependencyAggregate : ICrudServiceDependencyAggregate
{
    public IGenericDbContext DbContext { get; }
    public IMapper Mapper { get; }

    public GenericDbContextDependencyAggregate(IGenericDbContext dbContext, IMapper mapper)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
}
