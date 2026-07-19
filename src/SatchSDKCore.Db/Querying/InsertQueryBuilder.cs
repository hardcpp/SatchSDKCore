using System.Collections.Generic;
using SSC.Db.Attributes;

namespace SSC.Db.Querying;

/// <summary>
/// Builds an INSERT statement for a model.
/// </summary>
internal static class InsertQueryBuilder
{
    public static void BuildSingle(DbQuery query, object model)
    {
        DbModelMetadata metadata = query.DbModelMetadata!;
        query.Query.Append("INSERT INTO ");
        query.DbQueryDialect.Table(query);

        var fields = new List<DbFieldAttribute>();
        foreach (DbFieldAttribute field in metadata.FieldAttributes)
        {
            if (!field.AutoIncrement)
            {
                fields.Add(field);
            }
        }

        if (fields.Count == 0)
        {
            query.DbQueryDialect.DefaultValues(query);
            return;
        }

        query.Query.Append(" (");
        for (int i = 0; i < fields.Count; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            query.DbQueryDialect.FieldName(query, fields[i]);
        }

        query.Query.Append(") VALUES (");
        for (int i = 0; i < fields.Count; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            string parameterName = query.GenerateParameterName("Insert");
            query.AddParameter(parameterName, fields[i].FieldInfo.GetValue(model));
            query.Query.Append(parameterName);
        }

        query.Query.Append(')');
    }
}
