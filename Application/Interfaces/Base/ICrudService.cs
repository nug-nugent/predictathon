using FluentResults;

namespace Predictathon.Application.Interfaces.Base
{
    public interface ICrudService<TPrimaryKey, TModel, TEntity>
        where TPrimaryKey : struct, IComparable<TPrimaryKey>, IEquatable<TPrimaryKey>
        where TEntity : class
        where TModel : class
    {
        /// <summary>
        /// Create a new entity.
        /// </summary>
        /// <returns>A model representing the newly-created entity.</returns>
        Task<Result<TModel>> Create(TModel model);

        /// <summary>
        /// Get a model representing the entity by its ID.
        /// </summary>
        /// <returns>The model, or null if not found.</returns>
        Task<TModel?> GetById(TPrimaryKey id);

        /// <summary>
        /// Update the entity identified by <paramref name="id"/> using values from <paramref name="model"/>.
        /// </summary>
        /// <param name="id">Primary key of the entity to update.</param>
        /// <returns>A Result denoting success or failure, containing a model representing the updated TEntity</returns>
        Task<Result<TModel>> Update(TPrimaryKey id, TModel model);

        /// <summary>
        /// Delete the entity by its ID.
        /// </summary>
        /// <returns>A Result denoting success or failure.</returns>
        Task<Result> DeleteById(TPrimaryKey id);
    }
}