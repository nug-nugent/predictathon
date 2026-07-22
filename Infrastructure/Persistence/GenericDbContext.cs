using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces.Persistence;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
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

    // Explicit interface implementations: DbContext already declares AddAsync/Update/Remove with
    // different return types (ValueTask<EntityEntry<T>>/EntityEntry<T> instead of Task/void), so these
    // can't be `override`s. Implementing them explicitly keeps them off the class's public surface
    // entirely, reachable only via IGenericDbContext, instead of silently hiding the base members.
    async Task IGenericDbContext.AddAsync<T>(T entity, CancellationToken cancellationToken)
        => await Set<T>().AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        await Set<T>().AddRangeAsync(entities, cancellationToken);
    }

    void IGenericDbContext.Update<T>(T entity) => Set<T>().Update(entity);

    public void UpdateRange<T>(IEnumerable<T> entities) where T : class => Set<T>().UpdateRange(entities);

    void IGenericDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    public void RemoveRange<T>(IEnumerable<T> entities) where T : class => Set<T>().RemoveRange(entities);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex))
        {
            throw new DuplicateKeyException("A record with the same key already exists.", ex);
        }
    }

    /// <summary>
    /// Determines whether a <see cref="DbUpdateException"/> was caused by a SQL Server primary key
    /// or unique constraint violation (error 2627 or 2601).
    /// </summary>
    private static bool IsDuplicateKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601);

    public Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlRawAsync(sql, cancellationToken);

    public async Task CallStoredProcedureAsync(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        var openedByUs = await EnsureConnectionOpenAsync(connection, cancellationToken);

        try
        {
            await using var cmd = CreateStoredProcedureCommand(connection, storedProcedureName, parameters);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (openedByUs)
            {
                connection.Close();
            }
        }
    }

    public async Task<List<TReturnType>> CallStoredProcedureAsync<TReturnType>(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        where TReturnType : class, new()
    {
        var results = new List<TReturnType>();

        var connection = Database.GetDbConnection();
        var openedByUs = await EnsureConnectionOpenAsync(connection, cancellationToken);

        try
        {
            await using var cmd = CreateStoredProcedureCommand(connection, storedProcedureName, parameters);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var mappings = BuildColumnMappings<TReturnType>(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                var item = new TReturnType();

                foreach (var (setter, columnIndex) in mappings)
                {
                    var value = reader.GetValue(columnIndex);

                    if (value == DBNull.Value)
                    {
                        // Only reference types and Nullable<T> properties can actually hold null;
                        // leave non-nullable value-type properties at their default instead of throwing.
                        if (setter.CanBeNull)
                        {
                            setter.SetValue(item, null);
                        }

                        continue;
                    }

                    setter.SetValue(item, ConvertValue(value, setter.TargetType));
                }

                results.Add(item);
            }
        }
        finally
        {
            if (openedByUs)
            {
                connection.Close();
            }
        }

        return results;
    }

    /// <summary>
    /// Opens <paramref name="connection"/> if it isn't already open, returning whether this call opened it.
    /// Callers should only close a connection they themselves opened, rather than one already in use
    /// elsewhere on this DbContext (e.g. inside an ambient transaction).
    /// </summary>
    private static async Task<bool> EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State == ConnectionState.Open)
        {
            return false;
        }

        await connection.OpenAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Builds a stored-procedure command. The procedure name is passed as <see cref="DbCommand.CommandText"/>
    /// with <see cref="CommandType.StoredProcedure"/> rather than interpolated into SQL text, so it is
    /// never parsed as part of a SQL statement.
    /// </summary>
    private DbCommand CreateStoredProcedureCommand(DbConnection connection, string storedProcedureName, List<SqlParameter>? parameters)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = storedProcedureName;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Transaction = Database.CurrentTransaction?.GetDbTransaction();

        if (parameters is not null)
        {
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
        }

        return cmd;
    }

    /// <summary>
    /// Resolves, once per query, which result columns map onto which writable properties of
    /// <typeparamref name="TReturnType"/>, using an O(1) column-name lookup instead of re-scanning the
    /// column list for every property on every row.
    /// </summary>
    private static List<(PropertySetter Setter, int ColumnIndex)> BuildColumnMappings<TReturnType>(DbDataReader reader)
    {
        var columnIndexes = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnIndexes[reader.GetName(i)] = i;
        }

        var mappings = new List<(PropertySetter, int)>();
        foreach (var setter in GetPropertySetters(typeof(TReturnType)))
        {
            if (columnIndexes.TryGetValue(setter.Name, out var columnIndex))
            {
                mappings.Add((setter, columnIndex));
            }
        }

        return mappings;
    }

    private static readonly ConcurrentDictionary<Type, PropertySetter[]> PropertySetterCache = new();

    /// <summary>
    /// Compiled property setters for <paramref name="type"/>, built once per type and cached, avoiding
    /// repeated <see cref="PropertyInfo.SetValue(object?, object?)"/> reflection calls on every mapped row.
    /// </summary>
    private static PropertySetter[] GetPropertySetters(Type type)
        => PropertySetterCache.GetOrAdd(type, static t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .Select(p => new PropertySetter(p))
            .ToArray());

    /// <summary>
    /// Converts a non-null column value to <paramref name="targetType"/>. Handles BCL types that aren't
    /// <see cref="IConvertible"/> (and so aren't supported by <see cref="Convert.ChangeType(object, Type)"/>),
    /// such as <see cref="Guid"/>, <see cref="DateOnly"/>, <see cref="TimeOnly"/> and <see cref="TimeSpan"/>.
    /// </summary>
    private static object ConvertValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(Guid))
        {
            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value)!);
        }

        if (targetType.IsEnum)
        {
            return Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(DateOnly))
        {
            return DateOnly.FromDateTime((DateTime)value);
        }

        if (targetType == typeof(TimeOnly))
        {
            return value switch
            {
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                DateTime dt => TimeOnly.FromDateTime(dt),
                _ => throw new InvalidCastException($"Cannot convert value of type '{value.GetType()}' to {nameof(TimeOnly)}.")
            };
        }

        if (targetType == typeof(TimeSpan) && value is DateTime dateTimeValue)
        {
            return dateTimeValue.TimeOfDay;
        }

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// A compiled, cached setter for a single property, used in place of reflection-based
    /// <see cref="PropertyInfo.SetValue(object?, object?)"/> when mapping stored procedure result rows.
    /// </summary>
    private sealed class PropertySetter
    {
        private readonly Action<object, object?> _setValue;

        public string Name { get; }

        public Type TargetType { get; }

        public bool CanBeNull { get; }

        public PropertySetter(PropertyInfo property)
        {
            Name = property.Name;
            TargetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            CanBeNull = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null;
            _setValue = BuildSetter(property);
        }

        public void SetValue(object instance, object? value) => _setValue(instance, value);

        private static Action<object, object?> BuildSetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");

            var typedInstance = Expression.Convert(instanceParam, property.DeclaringType!);
            var typedValue = Expression.Convert(valueParam, property.PropertyType);
            var assign = Expression.Assign(Expression.Property(typedInstance, property), typedValue);

            return Expression.Lambda<Action<object, object?>>(assign, instanceParam, valueParam).Compile();
        }
    }
}
