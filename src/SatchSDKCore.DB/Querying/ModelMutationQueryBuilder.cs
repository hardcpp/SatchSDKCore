using System;
using SSC.DB.Attributes;

namespace SSC.DB.Querying;

/// <summary>
/// Shared query generation for model mutations.
/// </summary>
internal static class ModelMutationQueryBuilder
{
    public static void AppendPrimaryKeyCondition(
        DbQuery query,
        IDbModel model)
    {
        DbFieldAttribute[] primaryKeys = query.DbModelMetadata!.PrimaryFieldAttributes;
        if (primaryKeys.Length == 0)
        {
            throw new InvalidOperationException(
                $"Model '{query.DbModelMetadata!.Identifier}' must have a primary key for this operation");
        }

        query.Query.Append(" WHERE ");
        for (int i = 0; i < primaryKeys.Length; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(" AND ");
            }

            DbFieldAttribute field = primaryKeys[i];
            object? value = field.FieldInfo.GetValue(model);
            if (value == null)
            {
                throw new InvalidOperationException($"Primary key '{field.FieldInfo.Name}' cannot be null");
            }

            query.DbQueryDialect.Field(query, field);
            string parameterName = query.GenerateParameterName("Key");
            query.Query.Append(" = ").Append(parameterName);
            query.AddParameter(parameterName, value);
        }
    }
}
