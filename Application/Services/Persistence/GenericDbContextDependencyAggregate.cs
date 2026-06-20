using MapsterMapper;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Application.Services.Persistence;

[ScopedService]
public class GenericDbContextDependencyAggregate : ICrudServiceDependencyAggregate
{
    public required IGenericDbContext DbContext { get; set; }
    public required IMapper Mapper { get; set; }
}