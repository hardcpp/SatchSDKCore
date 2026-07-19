using SSC.DB.Expressions;
using SSC.DB.Querying;

namespace SSC.DB.Drivers;

/// <summary>
/// MySql database dialect implementation
/// </summary>
internal class MySqlDbDialect : IDbQueryDialect
{
    const string OBJECT_QUOTE = "`";

    public override void SelectAllFrom(DbQueryBuilder queryBuilder)
    {
        queryBuilder.Query.Append("SELECT ");
        for (var i = 0; i < queryBuilder.DbModelMetadata!.FieldAttributes.Length; ++i)
        {
            var field = queryBuilder.DbModelMetadata!.FieldAttributes[i];
            if (i > 0)
                queryBuilder.Query.Append(", ");

            Field(queryBuilder, field);
        }

        queryBuilder.Query.AppendFormat(" FROM {0}{1}{0}", OBJECT_QUOTE, queryBuilder.DbModelMetadata!.TableAttribute.TableName);
    }


    public override void Field(DbQueryBuilder queryBuilder, Attributes.DbFieldAttribute field)
    {
        queryBuilder.Query.AppendFormat("{0}{1}{0}.{0}{2}{0}", OBJECT_QUOTE, queryBuilder.DbModelMetadata!.TableAttribute.TableName, field.Name);
    }
    public override void FieldValueLower(DbQueryBuilder queryBuilder, Attributes.DbFieldAttribute field)
    {
        queryBuilder.Query.Append("LOWER(");
        Field(queryBuilder, field);
        queryBuilder.Query.Append(")");
    }
}
