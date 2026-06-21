using MapsterMapper;
using Predictathon.Application.Interfaces.Persistence;
using FluentValidation;

namespace Predictathon.Application.Interfaces.Base;

public interface ICrudServiceDependencyAggregate<TModel>
{
    IGenericDbContext DbContext { get; }
    IMapper Mapper { get; }
    IValidator<TModel>? Validator { get; }
}
