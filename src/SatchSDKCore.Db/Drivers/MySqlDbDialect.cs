using SSC.Db.Attributes;
using SSC.Db.Expressions;
using SSC.Db.Querying;

namespace SSC.Db.Drivers;

/// <summary>
/// MySql database dialect implementation
/// </summary>
internal class MySqlDbDialect : IDbQueryDialect
{
    private const string OBJECT_QUOTE = "`";

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override bool AutoIncrementResultIsSeparateQuery => true;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void SelectAllFrom(DbQuery query)
    {
        query.Query.Append("SELECT ");
        for (int i = 0; i < query.DbModelMetadata!.FieldAttributes.Length; ++i)
        {
            DbFieldAttribute field = query.DbModelMetadata!.FieldAttributes[i];
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            Field(query, field);
        }

        query.Query.Append(" FROM ");
        Table(query);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void Table(DbQuery query)
    {
        DbTableAttribute table = query.DbModelMetadata!.TableAttribute;
        if (!string.IsNullOrEmpty(table.Schema))
        {
            query.Query.AppendFormat("{0}{1}{0}.", OBJECT_QUOTE, table.Schema);
        }

        query.Query.AppendFormat("{0}{1}{0}", OBJECT_QUOTE, table.TableName);
    }

    public override void Field(DbQuery query, DbFieldAttribute field)
    {
        Table(query);
        query.Query.Append('.');
        FieldName(query, field);
    }

    public override void FieldName(DbQuery query, DbFieldAttribute field)
        => query.Query.AppendFormat("{0}{1}{0}", OBJECT_QUOTE, field.FieldName);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void DefaultValues(DbQuery query)
        => query.Query.Append(" () VALUES ()");

    public override void AutoIncrementResult(DbQuery query, DbFieldAttribute field)
        => query.Query.Append("SELECT LAST_INSERT_ID()");

    public override void FieldValueLower(DbQuery query, DbFieldAttribute field)
    {
        query.Query.Append("LOWER(");
        Field(query, field);
        query.Query.Append(")");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void StringConcatStart(DbQuery query)
        => query.Query.Append("CONCAT(");

    public override void StringConcatSeparator(DbQuery query)
        => query.Query.Append(", ");

    public override void StringConcatEnd(DbQuery query)
        => query.Query.Append(')');
}
