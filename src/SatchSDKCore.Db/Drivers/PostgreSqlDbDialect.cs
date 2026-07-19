using SSC.Db.Attributes;
using SSC.Db.Expressions;
using SSC.Db.Querying;

namespace SSC.Db.Drivers;

/// <summary>
/// PostgreSQL database dialect implementation.
/// </summary>
internal sealed class PostgreSqlDbDialect : IDbQueryDialect
{
    private const char ObjectQuote = '"';

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override bool AutoIncrementResultIsSeparateQuery => false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void SelectAllFrom(DbQuery query)
    {
        query.Query.Append("SELECT ");
        for (int i = 0; i < query.DbModelMetadata!.FieldAttributes.Length; ++i)
        {
            if (i > 0)
            {
                query.Query.Append(", ");
            }

            Field(query, query.DbModelMetadata.FieldAttributes[i]);
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
            query.Query.Append(ObjectQuote).Append(table.Schema).Append(ObjectQuote).Append('.');
        }

        query.Query.Append(ObjectQuote).Append(table.TableName).Append(ObjectQuote);
    }

    public override void Field(DbQuery query, DbFieldAttribute field)
    {
        Table(query);
        query.Query.Append('.');
        FieldName(query, field);
    }

    public override void FieldName(DbQuery query, DbFieldAttribute field)
        => query.Query.Append(ObjectQuote).Append(field.FieldName).Append(ObjectQuote);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void DefaultValues(DbQuery query)
        => query.Query.Append(" DEFAULT VALUES");

    public override void AutoIncrementResult(DbQuery query, DbFieldAttribute field)
    {
        query.Query.Append(" RETURNING ");
        FieldName(query, field);
    }

    public override void FieldValueLower(DbQuery query, DbFieldAttribute field)
    {
        query.Query.Append("LOWER(");
        Field(query, field);
        query.Query.Append(')');
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
