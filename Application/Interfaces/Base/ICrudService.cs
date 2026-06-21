using FluentResults;

namespace Predictathon.Application.Interfaces.Base
{
    public interface ICrudService<TPrimaryKey, TCreateModel, TEditModel, TEntity>
        where TPrimaryKey : struct, IComparable<TPrimaryKey>, IEquatable<TPrimaryKey>
        where TEntity : class
        where TCreateModel : class
        where TEditModel : class, new()
    {
        /// <summary>
        /// Create a new entity.
        /// </summary>
        /// <returns>A model representing the newly-created entity.</returns>
        Task<Result<TEditModel>> Create(TCreateModel model);

        /// <summary>
        /// Get a model representing the entity by its ID.
        /// </summary>
        /// <returns>The model, or null if not found.</returns>
        Task<TEditModel?> GetById(TPrimaryKey id);

        /// <summary>
        /// Update the entity identified by <paramref name="id"/> using values from <paramref name="model"/>.
        /// </summary>
        /// <param name="id">Primary key of the entity to update.</param>
        /// <returns>A Result denoting success or failure, containing a model representing the updated TEntity</returns>
        Task<Result<TEditModel>> Update(TPrimaryKey id, TEditModel model);

        /// <summary>
        /// Delete the entity by its ID.
        /// </summary>
        /// <returns>A Result denoting success or failure.</returns>
        Task<Result> DeleteById(TPrimaryKey id);
    }
}