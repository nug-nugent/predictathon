using MapsterMapper;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using FluentValidation;

namespace Predictathon.Application.Services;

public class CrudServiceDependencyAggregate<TCreateModel, TEditModel> : ICrudServiceDependencyAggregate<TCreateModel, TEditModel>
    where TCreateModel : class
    where TEditModel : class
{
    public IGenericDbContext DbContext { get; }
    public IMapper Mapper { get; }
    public IValidator<TCreateModel>? CreateValidator { get; }
    public IValidator<TEditModel>? EditValidator { get; }

    public CrudServiceDependencyAggregate(
        IGenericDbContext dbContext,
        IMapper mapper,
        IValidator<TCreateModel>? createValidator = null,
        IValidator<TEditModel>? editValidator = null)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        CreateValidator = createValidator;
        EditValidator = editValidator;
    }
}
