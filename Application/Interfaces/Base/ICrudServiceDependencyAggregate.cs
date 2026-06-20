using MapsterMapper;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Application.Interfaces.Base;

public interface ICrudServiceDependencyAggregate
{
    IGenericDbContext DbContext { get; }
    IMapper Mapper { get; }
}