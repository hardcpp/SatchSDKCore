using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SSC.Db.Attributes;

namespace SSC.Db;

/// <summary>
/// Strongly typed field assignments for a batch model update.
/// </summary>
/// <typeparam name="TModel">The model type whose fields can be assigned.</typeparam>
public sealed class DbUpdateSet<TModel>
    where TModel : IDbModel
{
    private readonly DbModelMetadata _metadata;

    internal DbUpdateSet(DbModelMetadata metadata)
    {
        _metadata = metadata;
    }

    internal List<KeyValuePair<DbFieldAttribute, object?>> Assignments { get; } = new();

    /// <summary>
    /// Assign a value to a mapped model field.
    /// </summary>
    /// <typeparam name="TValue">The selected field's CLR value type.</typeparam>
    /// <param name="fieldSelector">An expression selecting one directly mapped public field.</param>
    /// <param name="value">The value assigned to the selected field.</param>
    /// <returns>This update set, enabling fluent assignment chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fieldSelector" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// The selector is invalid, the field is not mutable, or the field was already
    /// assigned.
    /// </exception>
    public DbUpdateSet<TModel> Set<TValue>(Expression<Func<TModel, TValue>> fieldSelector, TValue value)
    {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        if (fieldSelector.Body is not MemberExpression { Member: FieldInfo fieldInfo } memberExpression
            || memberExpression.Expression is not ParameterExpression)
        {
            throw new ArgumentException("The selector must reference a model field directly", nameof(fieldSelector));
        }

        DbFieldAttribute? mappedField = null;
        foreach (DbFieldAttribute field in _metadata.FieldAttributes)
        {
            if (field.FieldInfo == fieldInfo)
            {
                mappedField = field;
                break;
            }
        }

        if (mappedField == null)
        {
            throw new ArgumentException($"Field '{fieldInfo.Name}' is not mapped on model '{_metadata.Identifier}'",
                nameof(fieldSelector));
        }

        if (mappedField.PrimaryKey || mappedField.AutoIncrement)
        {
            throw new ArgumentException($"Field '{fieldInfo.Name}' cannot be changed by a batch update",
                nameof(fieldSelector));
        }

        foreach (KeyValuePair<DbFieldAttribute, object?> assignment in Assignments)
        {
            if (assignment.Key == mappedField)
            {
                throw new ArgumentException($"Field '{fieldInfo.Name}' was assigned more than once",
                    nameof(fieldSelector));
            }
        }

        Assignments.Add(new KeyValuePair<DbFieldAttribute, object?>(mappedField, value));
        return this;
    }
}
