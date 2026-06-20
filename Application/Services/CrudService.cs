using FluentResults;
using MapsterMapper;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;

namespace Predictathon.Application.Services;

[ScopedService]
public class CrudService<TPrimaryKey, TModel, TEntity> : ICrudService<TPrimaryKey, TModel, TEntity>
    where TPrimaryKey : struct, IComparable<TPrimaryKey>, IEquatable<TPrimaryKey>
    where TEntity : class, new()
    where TModel : class
{
    private readonly IGenericDbContext _dbContext;
    private readonly IMapper _mapper;

    public CrudService(
        ICrudServiceDependencyAggregate dependencyAggregate
    )
    {
        _dbContext = dependencyAggregate.DbContext;
        _mapper = dependencyAggregate.Mapper;
    }

    /// <inheritdoc />
    public virtual async Task<Result<TModel>> Create(TModel model)
    {
        var entity = MapToEntity(model);

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
    public virtual Task<TModel?> GetById(TPrimaryKey id)
        => _dbContext.GetByIdAsync<TEntity>(id).ContinueWith(t => t.Result is null ? null : MapToModel(t.Result));

    /// <summary>
    /// Updates an existing entity using values from the provided model.
    /// By default this will locate the entity by calling <see cref="GetIdFromModel"/>,
    /// then copy matching writable properties from a mapped entity. Override <see cref="UpdateEntityFromModel"/>
    /// to provide custom behaviour.
    /// </summary>
    public virtual async Task<Result<TModel>> Update(TPrimaryKey id, TModel model)
    {
        var existing = await _dbContext.GetByIdAsync<TEntity>(id);

        if (existing is null)
        {
            return Result.Fail<TModel>("Entity not found");
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
    protected virtual TEntity MapToEntity(TModel model)
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
    protected virtual TModel MapToModel(TEntity entity)
    {
        return _mapper.Map<TModel>(entity);
    }

    /// <summary>
    /// Updates properties on <paramref name="entity"/> from <paramref name="model"/>.
    /// Override for custom update semantics.
    /// </summary>
    protected virtual void UpdateEntityFromModel(TEntity entity, TModel model)
    {
        _mapper.Map(model, entity);
        return;
    }
}
