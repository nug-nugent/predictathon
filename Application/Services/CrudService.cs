using FluentResults;
using FluentValidation;
using MapsterMapper;
using Predictathon.Application.Attributes;
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
    public virtual async Task<Result<TEditModel>> Create(TCreateModel model)
    {
        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(new FluentValidation.ValidationContext<TCreateModel>(model));
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new FluentResults.Error($"{e.PropertyName}: {e.ErrorMessage}")).ToArray();
                return Result.Fail<TEditModel>(errors);
            }
        }

        var editModel = new TEditModel();
        var entity = MapToEntity(editModel);

        await _dbContext.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        var updatedModel = MapToModel(entity);

        return Result.Ok(updatedModel);
    }

    /// <summary>
    /// Deletes the entity with the supplied id.
    /// </summary>
    public virtual async Task<Result> DeleteById(TPrimaryKey id)
    {
        var entity = await _dbContext.GetByIdAsync<TEntity>(id);

        if (entity is null)
        {
            return Result.Fail("Entity not found");
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync();

        return Result.Ok();
    }

    /// <summary>
    /// Returns the entity by id.
    /// </summary>
    public virtual Task<TEditModel?> GetById(TPrimaryKey id)
        => _dbContext.GetByIdAsync<TEntity>(id).ContinueWith(t => t.Result is null ? null : MapToModel(t.Result));

    /// <summary>
    /// Updates an existing entity using values from the provided model.
    /// By default this will locate the entity by calling <see cref="GetIdFromModel"/>,
    /// then copy matching writable properties from a mapped entity. Override <see cref="UpdateEntityFromModel"/>
    /// to provide custom behaviour.
    /// </summary>
    public virtual async Task<Result<TEditModel>> Update(TPrimaryKey id, TEditModel model)
    {
        if (_validator is not null)
        {
            var validation = await _validator.ValidateAsync(new FluentValidation.ValidationContext<TEditModel>(model));
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new FluentResults.Error($"{e.PropertyName}: {e.ErrorMessage}")).ToArray();
                return Result.Fail<TEditModel>(errors);
            }
        }

        var existing = await _dbContext.GetByIdAsync<TEntity>(id);

        if (existing is null)
        {
            return Result.Fail<TEditModel>("Entity not found");
        }

        UpdateEntityFromModel(existing, model);

        _dbContext.Update(existing);
        await _dbContext.SaveChangesAsync();

        return Result.Ok(MapToModel(existing));
    }

    /// <summary>
    /// Map a model to an entity.
    /// Override to provide explicit mapping logic.
    /// </summary>
    protected virtual TEntity MapToEntity(TEditModel model)
    {
        if (model is TEntity e)
        {
            return e;
        }

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
