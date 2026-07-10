using FluentResults;
using FluentValidation;
using MapsterMapper;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Application.Services;

[ScopedService]
public class CrudService<TPrimaryKey, TCreateModel, TEditModel, TEntity> : ICrudService<TPrimaryKey, TCreateModel, TEditModel, TEntity>
    where TPrimaryKey : struct, IComparable<TPrimaryKey>, IEquatable<TPrimaryKey>
    where TEntity : class, new()
    where TCreateModel : class
    where TEditModel : class, new()
{
    private readonly IGenericDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IValidator<TEditModel>? _validator;

    public CrudService(
        ICrudServiceDependencyAggregate<TEditModel> dependencyAggregate
    )
    {
        _dbContext = dependencyAggregate.DbContext;
        _mapper = dependencyAggregate.Mapper;
        _validator = dependencyAggregate.Validator;
    }

    /// <inheritdoc />
    public virtual async Task<Result<TEditModel>> Create(TCreateModel model, CancellationToken cancellationToken = default)
    {
        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(new FluentValidation.ValidationContext<TCreateModel>(model), cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(ToValidationError).ToArray();
                return Result.Fail<TEditModel>(errors);
            }
        }

        var entity = MapToEntity(model);

        await _dbContext.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedModel = MapToModel(entity);

        return Result.Ok(updatedModel);
    }

    /// <summary>
    /// Returns the entity by id.
    /// </summary>
    public virtual async Task<TEditModel?> GetById(TPrimaryKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.GetByIdAsync<TEntity>(id, cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <summary>
    /// Updates an existing entity using values from the provided model.
    /// By default this will locate the entity by calling <see cref="GetIdFromModel"/>,
    /// then copy matching writable properties from a mapped entity. Override <see cref="UpdateEntityFromModel"/>
    /// to provide custom behaviour.
    /// </summary>
    public virtual async Task<Result<TEditModel>> Update(TPrimaryKey id, TEditModel model, CancellationToken cancellationToken = default)
    {
        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(new FluentValidation.ValidationContext<TEditModel>(model), cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(ToValidationError).ToArray();
                return Result.Fail<TEditModel>(errors);
            }
        }

        var existing = await _dbContext.GetByIdAsync<TEntity>(id, cancellationToken);

        if (existing is null)
        {
            return Result.Fail<TEditModel>(new NotFoundError());
        }

        UpdateEntityFromModel(existing, model);

        _dbContext.Update(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(MapToModel(existing));
    }

    /// <summary>
    /// Deletes the entity with the supplied id.
    /// </summary>
    public virtual async Task<Result> DeleteById(TPrimaryKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.GetByIdAsync<TEntity>(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(new NotFoundError());
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>
    /// Converts a FluentValidation failure into a <see cref="PropertyValidationError"/> so
    /// callers can group errors by property without parsing the error message text.
    /// </summary>
    private static PropertyValidationError ToValidationError(FluentValidation.Results.ValidationFailure failure)
        => new(failure.PropertyName, failure.ErrorMessage);

    /// <summary>
    /// Map a model to an entity.
    /// Override to provide explicit mapping logic.
    /// </summary>
    protected virtual TEntity MapToEntity(TEditModel model)
    {
        return _mapper.Map<TEntity>(model);
    }

    /// <summary>
    /// Map a model to an entity.
    /// Override to provide explicit mapping logic.
    /// </summary>
    protected virtual TEntity MapToEntity(TCreateModel model)
    {
        return _mapper.Map<TEntity>(model);
    }

    /// <summary>
    /// Map an entity to a model.
    /// Override to provide explicit mapping logic.
    /// </summary>
    protected virtual TEditModel MapToModel(TEntity entity)
    {
        return _mapper.Map<TEditModel>(entity);
    }

    /// <summary>
    /// Updates properties on <paramref name="entity"/> from <paramref name="model"/>.
    /// Override for custom update semantics.
    /// </summary>
    protected virtual void UpdateEntityFromModel(TEntity entity, TEditModel model)
    {
        _mapper.Map(model, entity);
        return;
    }
}
