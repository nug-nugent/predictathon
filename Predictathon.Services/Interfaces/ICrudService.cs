using FluentResults;

namespace Predictathon.Application.Interfaces
{
    public interface ICrudService<TPrimaryKey, TModel, TEntity>
        where TPrimaryKey : struct, IComparable<TPrimaryKey>, IEquatable<TPrimaryKey>
        where TEntity : class
        where TModel : class
    {
        /// <summary>
        /// Create a new entity.
        /// </summary>
        /// <returns>The newly-created entity.</returns>
        Task<Result<TEntity>> Create(TModel model);

        /// <summary>
        /// Get the entity by its ID.
        /// </summary>
        /// <returns>The entity, or null if not found.</returns>
        Task<TEntity?> GetById(TPrimaryKey id);

        /// <summary>
        /// Update the entity.
        /// </summary>
        /// <returns>A Result denoting success or failure, containing the updated TEntity</returns>
        Task<Result<TEntity>> Update(TModel model);

        /// <summary>
        /// Delete the entity by its ID.
        /// </summary>
        /// <returns>A Result denoting success or failure.</returns>
        Task<Result> DeleteById(TPrimaryKey id);
    }
}
