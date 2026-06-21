using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Interfaces.Persistence;
using System.Linq.Expressions;
using System.Data;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Predictathon.Infrastructure.Persistence;

public class GenericDbContext<TContext> : DbContext, IGenericDbContext
    where TContext : DbContext
{
    public GenericDbContext(DbContextOptions<TContext> options) : base(options)
    { }

    public IQueryable<T> Query<T>() where T : class => Set<T>().AsQueryable();

    public async Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
        => await Set<T>().FindAsync([id], cancellationToken);

    public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
        => await Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
    {
        if (predicate is null)
        {
            return await Set<T>().ToListAsync(cancellationToken);
        }

        return await Set<T>().Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        await Set<T>().AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        await Set<T>().AddRangeAsync(entities, cancellationToken);
    }

    public void Update<T>(T entity) where T : class => Set<T>().Update(entity);

    public void UpdateRange<T>(IEnumerable<T> entities) where T : class => Set<T>().UpdateRange(entities);

    public void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);

    public void RemoveRange<T>(IEnumerable<T> entities) where T : class => Set<T>().RemoveRange(entities);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => base.SaveChangesAsync(cancellationToken);

    public Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlRawAsync(sql, cancellationToken);

    public Task CallStoredProcedureAsync(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        // Build command like: EXEC procName @p1, @p2
        var paramPlaceholders = parameters != null && parameters.Count > 0
            ? string.Join(", ", parameters.Select(p => p.ParameterName))
            : string.Empty;

        var command = string.IsNullOrWhiteSpace(paramPlaceholders)
            ? $"EXEC {storedProcedureName}"
            : $"EXEC {storedProcedureName} {paramPlaceholders}";

        var paramArray = parameters?.ToArray() ?? Array.Empty<object>();

        return Database.ExecuteSqlRawAsync(command, paramArray, cancellationToken);
    }

    public async Task<List<TReturnType>> CallStoredProcedureAsync<TReturnType>(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        where TReturnType : class, new()
    {
        var results = new List<TReturnType>();

        var connection = Database.GetDbConnection();
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters is not null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var props = typeof(TReturnType).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columnNames = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();

            while (await reader.ReadAsync(cancellationToken))
            {
                var item = new TReturnType();

                foreach (var prop in props)
                {
                    var colIndex = columnNames.FindIndex(n => string.Equals(n, prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (colIndex < 0) continue;

                    var value = reader.GetValue(colIndex);
                    if (value == DBNull.Value)
                    {
                        prop.SetValue(item, null);
                        continue;
                    }

                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    object? safeValue;

                    if (targetType == typeof(Guid))
                    {
                        safeValue = reader.GetGuid(colIndex);
                    }
                    else if (targetType.IsEnum)
                    {
                        safeValue = Enum.ToObject(targetType, value);
                    }
                    else
                    {
                        safeValue = Convert.ChangeType(value, targetType);
                    }

                    prop.SetValue(item, safeValue);
                }

                results.Add(item);
            }
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
            {
                try { connection.Close(); } catch { }
            }
        }

        return results;
    }
}
