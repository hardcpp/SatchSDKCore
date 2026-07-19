using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SSC.DB.Attributes;
using SSC.DB.Expressions;

namespace SSC.DB.Querying;

/// <summary>
/// Builds UPDATE statements for models.
/// </summary>
internal static class UpdateQueryBuilder
{
    public static void BuildSingle(DbQuery query, IDbModel model)
    {
        AppendUpdate(query, model);
        ModelMutationQueryBuilder.AppendPrimaryKeyCondition(query, model);
    }

    public static void BuildMulti(
        DbQuery query,
        IReadOnlyList<KeyValuePair<DbFieldAttribute, object?>> assignments,
        Expression? condition)
    {
        query.Query.Append("UPDATE ");
        query.DbQueryDialect.Table(query);
        query.Query.Append(" SET ");
        for (int i = 0; i < assignments.Count; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            KeyValuePair<DbFieldAttribute, object?> assignment = assignments[i];
            query.DbQueryDialect.FieldName(query, assignment.Key);
            string parameterName = query.GenerateParameterName("Update");
            query.Query.Append(" = ").Append(parameterName);
            query.AddParameter(parameterName, assignment.Value);
        }

        if (condition != null)
        {
            WhereClauseBuilder.Build(query, condition);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private static void AppendUpdate(DbQuery query, object model)
    {
        DbModelMetadata metadata = query.DbModelMetadata!;
        var fields = new List<DbFieldAttribute>();
        foreach (DbFieldAttribute field in metadata.FieldAttributes)
        {
            if (field is { PrimaryKey: false, AutoIncrement: false })
            {
                fields.Add(field);
            }
        }

        if (fields.Count == 0)
        {
            throw new InvalidOperationException($"Model '{metadata.Identifier}' has no mutable fields to update");
        }

        query.Query.Append("UPDATE ");
        query.DbQueryDialect.Table(query);
        query.Query.Append(" SET ");
        for (int i = 0; i < fields.Count; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            DbFieldAttribute field = fields[i];
            query.DbQueryDialect.FieldName(query, field);
            string parameterName = query.GenerateParameterName("Update");
            query.Query.Append(" = ").Append(parameterName);
            query.AddParameter(parameterName, field.FieldInfo.GetValue(model));
        }
    }
}
