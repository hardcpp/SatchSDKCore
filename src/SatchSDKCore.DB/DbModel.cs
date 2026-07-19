using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using SSC.DB.Attributes;
using SSC.DB.Expressions;
using SSC.DB.Querying;

namespace SSC.DB;

/// <summary>
/// DB model base specialized class
/// </summary>
/// <typeparam name="TModel">Type of the model</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public class DbModel
    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TModel>
    : IDbModel
    where TModel : IDbModel, new()
{
    /// <inheritdoc />
    public static DbModelMetadata Metadata { get; } = DbModelMetadata.Get<TModel>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get the database instance attached to this model.
    /// </summary>
    /// <returns>The configured database instance.</returns>
    public static DbInstance GetDbInstance()
        => DbInstance.Get(Metadata.TableAttribute.DbInstanceName);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Insert this model and populate its auto-increment field, when present.
    /// </summary>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The number of inserted rows.</returns>
    public int Insert(IDbSession? dbSession = null)
    {
        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            InsertQueryBuilder.BuildSingle(queryBuilder, this);

            if (Metadata.AutoIncrementField == null)
            {
                return queryBuilder.ExecuteNonQuery();
            }

            if (queryBuilder.DbQueryDialect.AutoIncrementResultIsSeparateQuery)
            {
                int affectedRows = queryBuilder.ExecuteNonQuery();
                if (affectedRows != 1)
                {
                    throw new DataException(
                        $"Insert affected {affectedRows} rows for model '{Metadata.Identifier}' instead of one");
                }

                queryBuilder.Reset(fixedDbSession, Metadata);
            }

            queryBuilder.DbQueryDialect.AutoIncrementResult(queryBuilder, Metadata.AutoIncrementField);
            object? generatedValue = queryBuilder.ExecuteScalar();
            if (generatedValue == null || generatedValue == DBNull.Value)
            {
                throw new DataException(
                    $"Insert did not return an auto-increment value for model '{Metadata.Identifier}'");
            }

            Metadata.AutoIncrementField.FieldInfo.SetValue(
                this,
                ConvertFieldValue(Metadata.AutoIncrementField, generatedValue)
            );

            return 1;
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    /// <summary>
    /// Update all mutable fields, identifying this model by all of its primary keys.
    /// </summary>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The number of updated rows.</returns>
    public int Update(IDbSession? dbSession = null)
    {
        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            UpdateQueryBuilder.BuildSingle(queryBuilder, this);

            return queryBuilder.ExecuteNonQuery();
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    /// <summary>
    /// Delete this model, identifying it by all of its primary keys.
    /// </summary>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The number of deleted rows.</returns>
    public int Delete(IDbSession? dbSession = null)
    {
        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            DeleteQueryBuilder.BuildSingle(queryBuilder, this);

            return queryBuilder.ExecuteNonQuery();
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Select and materialize all matching rows.
    /// </summary>
    /// <param name="condition">An optional predicate used to filter rows.</param>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The materialized models in database result order.</returns>
    public static List<TModel> Select(
        Expression<Func<TModel, bool>>? condition = null,
        IDbSession? dbSession = null
    )
    {
        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            fixedDbSession.DbInstance.DbDialect.SelectAllFrom(queryBuilder);

            if (condition != null)
            {
                WhereClauseBuilder.Build(queryBuilder, condition.Body);
            }

            var result = new List<TModel>();
            using DbDataReader reader = queryBuilder.ExecuteReader();
            while (reader.Read())
            {
                var model = new TModel();
                for (int i = 0; i < Metadata.FieldAttributes.Length; ++i)
                {
                    DbFieldAttribute field = Metadata.FieldAttributes[i];
                    object value = reader.GetValue(i);
                    field.FieldInfo.SetValue(model, ConvertFieldValue(field, value));
                }

                result.Add(model);
            }

            return result;
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    /// <summary>
    /// Update selected fields using strongly typed field expressions.
    /// A null condition updates the entire table.
    /// </summary>
    /// <param name="configure">Configures one or more strongly typed field assignments.</param>
    /// <param name="condition">An optional predicate; <see langword="null" /> updates every row.</param>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The number of updated rows.</returns>
    public static int Update(
        Action<DbUpdateSet<TModel>> configure,
        Expression<Func<TModel, bool>>? condition = null,
        IDbSession? dbSession = null
    )
    {
        ArgumentNullException.ThrowIfNull(configure);

        var updateSet = new DbUpdateSet<TModel>(Metadata);
        configure(updateSet);
        if (updateSet.Assignments.Count == 0)
        {
            throw new ArgumentException("At least one field must be assigned", nameof(configure));
        }

        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            UpdateQueryBuilder.BuildMulti(queryBuilder, updateSet.Assignments, condition?.Body);

            return queryBuilder.ExecuteNonQuery();
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    /// <summary>
    /// Delete every row matching an optional condition.
    /// A null condition deletes the entire table.
    /// </summary>
    /// <param name="condition">An optional predicate; <see langword="null" /> deletes every row.</param>
    /// <param name="dbSession">
    /// An optional caller-owned session; when omitted, a thread-local session is acquired and
    /// released.
    /// </param>
    /// <returns>The number of deleted rows.</returns>
    public static int Delete(
        Expression<Func<TModel, bool>>? condition = null,
        IDbSession? dbSession = null
    )
    {
        IDbSession fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        try
        {
            DbQuery queryBuilder = DbQuery.GetTlsSingleton();
            queryBuilder.Reset(fixedDbSession, Metadata);

            DeleteQueryBuilder.BuildMulti(queryBuilder, condition?.Body);

            return queryBuilder.ExecuteNonQuery();
        }
        finally
        {
            if (dbSession == null)
            {
                fixedDbSession.Dispose();
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private static object? ConvertFieldValue(DbFieldAttribute field, object value)
    {
        Type fieldType = field.FieldType;
        Type targetType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

        if (value == DBNull.Value)
        {
            if (!fieldType.IsValueType || Nullable.GetUnderlyingType(fieldType) != null)
            {
                return null;
            }

            throw new DataException(
                $"Database returned NULL for non-nullable field '{field.FieldName}' on model '{Metadata.Identifier}'");
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            if (value is string enumName)
            {
                return Enum.Parse(targetType, enumName, false);
            }

            Type underlyingType = Enum.GetUnderlyingType(targetType);
            return Enum.ToObject(targetType, Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture)!);
        }

        if (targetType == typeof(Guid) && value is string guid)
        {
            return Guid.Parse(guid);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return the model name and all mapped field values.
    /// </summary>
    /// <returns>A diagnostic representation with sensitive fields redacted.</returns>
    public override string ToString()
    {
        StringBuilder result = new StringBuilder(Metadata.ModelType.Name).Append(" { ");
        for (int i = 0; i < Metadata.FieldAttributes.Length; ++i)
        {
            if (i > 0)
            {
                result.Append(", ");
            }

            DbFieldAttribute field = Metadata.FieldAttributes[i];
            result.Append(field.FieldInfo.Name).Append(" = ");
            if (field.Sensitive)
            {
                result.Append("<redacted>");
            }
            else
            {
                object? value = field.FieldInfo.GetValue(this);
                switch (value)
                {
                    case null:
                        result.Append("null");
                        break;

                    case string text:
                        result.Append('"')
                            .Append(text.Replace("\\", "\\\\", StringComparison.Ordinal)
                                .Replace("\"", "\\\"", StringComparison.Ordinal)
                                .Replace("\r", "\\r", StringComparison.Ordinal)
                                .Replace("\n", "\\n", StringComparison.Ordinal))
                            .Append('"');
                        break;

                    case char character:
                        result.Append('\'').Append(character).Append('\'');
                        break;

                    case IFormattable formattable:
                        result.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                        break;

                    default:
                        result.Append(value);
                        break;
                }
            }
        }

        return result.Append(" }").ToString();
    }
}
