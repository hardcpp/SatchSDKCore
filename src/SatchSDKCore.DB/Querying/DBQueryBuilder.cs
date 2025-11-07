using SSC.DB.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace SSC.DB.Querying;

/// <summary>
/// DB query builder
/// </summary>
public class DBQueryBuilder
{
    private static ThreadLocal<DBQueryBuilder?> m_Instances = new(() => new());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public          DBSession                   DBSession;
    public          DBInstance                  DBInstance;
    public          IDBQueryDialect             DBQueryDialect;
    public          DBModelMetadata?            DBModelMetadata;
    public readonly StringBuilder               Query;
    public readonly Dictionary<string, object>  Parameters = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private DbCommand               m_DbCommand                 = null!;
    private Dictionary<string, int> m_ParameterNameGenerator    = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a thread local storage instance
    /// </summary>
    /// <returns></returns>
    public static DBQueryBuilder GetTLSSingleton()
        => m_Instances.Value!;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public DBQueryBuilder()
    {
        DBSession       = null!;
        DBInstance      = null!;
        DBQueryDialect  = null!;
        Query           = new StringBuilder(2048);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Reset this query builder
    /// </summary>
    /// <param name="dbSession">DB session</param>
    /// <param name="dBModelMetadata">DB model metadata</param>
    public void Reset(DBSession dbSession, DBModelMetadata? dBModelMetadata)
    {
        if (m_DbCommand != null)
            m_DbCommand.Cancel();

        DBSession       = dbSession;
        DBInstance      = dbSession.DBInstance;
        DBQueryDialect  = dbSession.DBInstance.DBDialect;
        DBModelMetadata = dBModelMetadata;

        m_DbCommand = dbSession.CreateDbCommand();
        m_ParameterNameGenerator.Clear();

        Query.Clear();
        Parameters.Clear();
    }

    public int ExecuteNonQuery()
    {
        PrepareCommand();
        return m_DbCommand.ExecuteNonQuery();
    }
    public DbDataReader ExecuteReader()
    {
        PrepareCommand();
        return m_DbCommand.ExecuteReader();
    }
    public object? ExecuteScalar()
    {
        PrepareCommand();
        return m_DbCommand.ExecuteScalar();
    }

    /*
        public abstract int ExecuteNonQuery();
        public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken);
        public Task<int> ExecuteNonQueryAsync();
        public DbDataReader ExecuteReader();
        public DbDataReader ExecuteReader(CommandBehavior behavior);
        public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken);
        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior);
        public Task<DbDataReader> ExecuteReaderAsync();
        public abstract object? ExecuteScalar();
        public virtual Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken);
        public Task<object?> ExecuteScalarAsync();

    */

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Generate a parameter name with an optional prefix, increment a stored counter
    /// </summary>
    /// <param name="prefix">Optional prefix for the parameter name</param>
    /// <returns>Generated parameter name</returns>
    public string GenerateParameterName(string? prefix)
    {
        if (prefix == null)
            prefix = string.Empty;

        if (!m_ParameterNameGenerator.ContainsKey(prefix))
            m_ParameterNameGenerator.Add(prefix, 0);

        var index = ++m_ParameterNameGenerator[prefix];

        return $"@{prefix}{index}";
    }
    /// <summary>
    /// Add a parameter
    /// </summary>
    /// <param name="name">Name of the parameter</param>
    /// <param name="value">Value of the parameter</param>
    /// <exception cref="Exception">If a parameter with the same name already exists</exception>
    public void AddParameter(string name, object? value)
    {
        if (m_DbCommand == null)
            throw new Exception("The query builder was not reset!");

        ArgumentNullException.ThrowIfNull(name);

        if (m_DbCommand.Parameters.IndexOf(name) != -1)
            throw new Exception($"An argument with the name '{name}' is already existing");

        var l_Parameter = m_DbCommand.CreateParameter();
        l_Parameter.ParameterName   = name;
        l_Parameter.Value           = value;

        m_DbCommand.Parameters.Add(l_Parameter);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Complete the query
    /// </summary>
    private void PrepareCommand()
    {
        ArgumentNullException.ThrowIfNull(m_DbCommand);

        m_DbCommand.CommandText = Query.ToString();
        m_DbCommand.Prepare();
    }
}
