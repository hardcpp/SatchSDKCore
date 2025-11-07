using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace SSC.DB;

/// <summary>
/// DB model base specialized class
/// </summary>
/// <typeparam name="t_Type">Type of the model</typeparam>
public class DBModel
    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] t_Type>
    : IDBModel
    where t_Type : IDBModel
{
    /// <summary>
    /// Auto register static constructor
    /// </summary>
    static DBModel()
    {
        Metadata = DBModelMetadata.Get<t_Type>();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public static List<t_Type> Select(Expression<Func<t_Type, bool>>? condition = null, DBSession? dbSession = null)
    {
        var l_DBSession     = dbSession ?? GetDBInstance().GetTLSSession();
        var l_QueryBuilder  = Querying.DBQueryBuilder.GetTLSSingleton();

        l_QueryBuilder.Reset(l_DBSession, Metadata!);
        l_DBSession.DBInstance.DBDialect.SelectAllFrom(l_QueryBuilder);

        if (condition != null)
            Expressions.WhereClauseBuilder.Build(l_QueryBuilder, condition.Body);

        Console.WriteLine(l_QueryBuilder.Query.ToString());
        var ee= l_QueryBuilder.ExecuteReader();
        ee.Close();

        if (dbSession == null)
            l_DBSession.Dispose();

        return null;
    }
}
