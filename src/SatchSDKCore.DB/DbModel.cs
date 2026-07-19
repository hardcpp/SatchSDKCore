using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace SSC.DB;

/// <summary>
/// DB model base specialized class
/// </summary>
/// <typeparam name="TModel">Type of the model</typeparam>
public class DbModel
    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TModel>
    : IDbModel
    where TModel : IDbModel
{
    /// <summary>
    /// Auto register static constructor
    /// </summary>
    static DbModel()
    {
        Metadata = DbModelMetadata.Get<TModel>();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public static List<TModel> Select(Expression<Func<TModel, bool>>? condition = null, IDbSession? dbSession = null)
    {
        var fixedDbSession = dbSession ?? GetDbInstance().GetTlsSession();
        var queryBuilder = Querying.DbQueryBuilder.GetTlsSingleton();

        queryBuilder.Reset(fixedDbSession, Metadata!);
        fixedDbSession.DbInstance.DbDialect.SelectAllFrom(queryBuilder);

        if (condition != null)
            Expressions.WhereClauseBuilder.Build(queryBuilder, condition.Body);

        Console.WriteLine(queryBuilder.Query.ToString());
        var ee= queryBuilder.ExecuteReader();
        ee.Close();

        if (dbSession == null)
            fixedDbSession.Dispose();

        return null;
    }
}
