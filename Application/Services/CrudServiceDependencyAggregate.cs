using MapsterMapper;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using FluentValidation;

namespace Predictathon.Application.Services;

public class CrudServiceDependencyAggregate<TModel> : ICrudServiceDependencyAggregate<TModel>
    where TModel : class
{
    public IGenericDbContext DbContext { get; }
    public IMapper Mapper { get; }
    public IValidator<TModel>? Validator { get; }

    public CrudServiceDependencyAggregate(IGenericDbContext dbContext, IMapper mapper, IValidator<TModel>? validator = null)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        Validator = validator;
    }
}
