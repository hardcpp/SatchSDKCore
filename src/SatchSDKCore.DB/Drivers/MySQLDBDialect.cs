using SSC.DB.Expressions;
using SSC.DB.Querying;

namespace SSC.DB.Drivers;

/// <summary>
/// MySql database dialect implementation
/// </summary>
internal class MySqlDBDialect : IDBQueryDialect
{
    const string OBJECT_QUOTE = "`";

    public override void SelectAllFrom(DBQueryBuilder queryBuilder)
    {
        queryBuilder.Query.Append("SELECT ");
        for (var l_I = 0; l_I < queryBuilder.DBModelMetadata!.FieldAttributes.Length; ++l_I)
        {
            var l_Field = queryBuilder.DBModelMetadata!.FieldAttributes[l_I];
            if (l_I > 0)
                queryBuilder.Query.Append(", ");

            Field(queryBuilder, l_Field);
        }

        queryBuilder.Query.AppendFormat(" FROM {0}{1}{0}", OBJECT_QUOTE, queryBuilder.DBModelMetadata!.TableAttribute.TableName);
    }


    public override void Field(DBQueryBuilder queryBuilder, Attributes.DBFieldAttribute field)
    {
        queryBuilder.Query.AppendFormat("{0}{1}{0}.{0}{2}{0}", OBJECT_QUOTE, queryBuilder.DBModelMetadata!.TableAttribute.TableName, field.Name);
    }
    public override void FieldValueLower(DBQueryBuilder queryBuilder, Attributes.DBFieldAttribute field)
    {
        queryBuilder.Query.Append("LOWER(");
        Field(queryBuilder, field);
        queryBuilder.Query.Append(")");
    }
}
