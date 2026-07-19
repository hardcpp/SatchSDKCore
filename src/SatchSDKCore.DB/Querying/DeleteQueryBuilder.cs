using System.Linq.Expressions;
using SSC.DB.Expressions;

namespace SSC.DB.Querying;

/// <summary>
/// Builds DELETE statements for models.
/// </summary>
internal static class DeleteQueryBuilder
{
    public static void BuildSingle(DbQuery query, IDbModel model)
    {
        AppendDelete(query);
        ModelMutationQueryBuilder.AppendPrimaryKeyCondition(query, model);
    }

    public static void BuildMulti(DbQuery query, Expression? condition)
    {
        AppendDelete(query);
        if (condition != null)
        {
            WhereClauseBuilder.Build(query, condition);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private static void AppendDelete(DbQuery query)
    {
        query.Query.Append("DELETE FROM ");
        query.DbQueryDialect.Table(query);
    }
}
