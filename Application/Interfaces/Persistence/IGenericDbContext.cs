using Microsoft.Data.SqlClient;
using System.Linq.Expressions;

namespace Predictathon.Application.Interfaces.Persistence;

/// <summary>
/// Abstraction over a persistence context allowing basic querying and CRUD operations.
/// Implementations should translate <see cref="Expression{Func{T, bool}}"/> predicates to provider-specific queries
/// (for example EF Core will translate to SQL) so filters execute server-side when possible.
/// </summary>
public interface IGenericDbContext
{
    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> that can be used to build LINQ queries against the underlying store.
    /// The returned query should be executed by the caller (e.g. via ToListAsync, FirstOrDefaultAsync, etc.).
    /// </summary>
    IQueryable<T> Query<T>() where T : class;

    /// <summary>
    /// Finds an entity by its primary key value.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="id">Primary key value. The exact expected type/shape depends on the entity mapping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise <c>null</c>.</returns>
    Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Returns the first entity that matches the provided predicate, or <c>null</c> if none match.
    /// Use an <see cref="Expression{Func{T, bool}}"/> so providers can translate the filter to the store query language.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="predicate">Filter expression applied by the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Returns a read-only list of entities that match the optional predicate. If <paramref name="predicate"/> is <c>null</c>
    /// all entities of type <typeparamref name="T"/> should be returned.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="predicate">Optional filter expression applied by the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Adds a new entity to the context. The change is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Adds multiple entities to the context. The changes are persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Marks an entity as modified. Changes are persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    void Update<T>(T entity) where T : class;

    /// <summary>
    /// Marks multiple entities as modified.
    /// </summary>
    void UpdateRange<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Marks an entity for deletion. The deletion is executed when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    void Remove<T>(T entity) where T : class;

    /// <summary>
    /// Marks multiple entities for deletion.
    /// </summary>
    void RemoveRange<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Persists all changes made in the context to the underlying store.
    /// Returns the number of state entries written to the store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL command against the underlying store. Use with care to avoid SQL injection.
    /// </summary>
    /// <param name="sql">SQL command text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure by name with optional parameters.
    /// </summary>
    /// <param name="storedProcedureName">Stored procedure name (including schema if required).</param>
    /// <param name="parameters">Optional list of parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CallStoredProcedureAsync(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure and maps the resulting rows to instances of <typeparamref name="TReturnType"/>.
    /// Property names on <typeparamref name="TReturnType"/> are matched to column names (case-insensitive).
    /// </summary>
    Task<List<TReturnType>> CallStoredProcedureAsync<TReturnType>(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        where TReturnType : class, new();
}
