using MapsterMapper;
using Predictathon.Application.Interfaces.Persistence;
using FluentValidation;

namespace Predictathon.Application.Interfaces.Base;

public interface ICrudServiceDependencyAggregate<TCreateModel, TEditModel>
{
    IGenericDbContext DbContext { get; }
    IMapper Mapper { get; }
    IValidator<TCreateModel>? CreateValidator { get; }
    IValidator<TEditModel>? EditValidator { get; }
}
