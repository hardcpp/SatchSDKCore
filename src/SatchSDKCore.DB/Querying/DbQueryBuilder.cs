using SSC.DB.Expressions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace SSC.DB.Querying;

/// <summary>
/// DB query builder
/// </summary>
public class DbQueryBuilder
{
    private static ThreadLocal<DbQueryBuilder?> _instances = new(() => new());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public          IDbSession                   DbSession;
    public          DbInstance                  DbInstance;
    public          IDbQueryDialect             DbQueryDialect;
    public          DbModelMetadata?            DbModelMetadata;
    public readonly StringBuilder               Query;
    public readonly Dictionary<string, object>  Parameters = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private DbCommand               _dbCommand                 = null!;
    private Dictionary<string, int> _parameterNameGenerator    = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a thread local storage instance
    /// </summary>
    /// <returns></returns>
    public static DbQueryBuilder GetTlsSingleton()
        => _instances.Value!;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public DbQueryBuilder()
    {
        DbSession       = null!;
        DbInstance      = null!;
        DbQueryDialect  = null!;
        Query           = new StringBuilder(2048);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Reset this query builder
    /// </summary>
    /// <param name="dbSession">DB session</param>
    /// <param name="dBModelMetadata">DB model metadata</param>
    public void Reset(IDbSession dbSession, DbModelMetadata? dBModelMetadata)
    {
        if (_dbCommand != null)
            _dbCommand.Cancel();

        DbSession       = dbSession;
        DbInstance      = dbSession.DbInstance;
        DbQueryDialect  = dbSession.DbInstance.DbDialect;
        DbModelMetadata = dBModelMetadata;

        _dbCommand = dbSession.CreateDbCommand();
        _parameterNameGenerator.Clear();

        Query.Clear();
        Parameters.Clear();
    }

    public int ExecuteNonQuery()
    {
        PrepareCommand();
        return _dbCommand.ExecuteNonQuery();
    }
    public DbDataReader ExecuteReader()
    {
        PrepareCommand();
        return _dbCommand.ExecuteReader();
    }
    public object? ExecuteScalar()
    {
        PrepareCommand();
        return _dbCommand.ExecuteScalar();
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

        if (!_parameterNameGenerator.ContainsKey(prefix))
            _parameterNameGenerator.Add(prefix, 0);

        var index = ++_parameterNameGenerator[prefix];

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
        if (_dbCommand == null)
            throw new Exception("The query builder was not reset!");

        ArgumentNullException.ThrowIfNull(name);

        if (_dbCommand.Parameters.IndexOf(name) != -1)
            throw new Exception($"An argument with the name '{name}' is already existing");

        var l_Parameter = _dbCommand.CreateParameter();
        l_Parameter.ParameterName   = name;
        l_Parameter.Value           = value;

        _dbCommand.Parameters.Add(l_Parameter);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Complete the query
    /// </summary>
    private void PrepareCommand()
    {
        ArgumentNullException.ThrowIfNull(_dbCommand);

        _dbCommand.CommandText = Query.ToString();
        _dbCommand.Prepare();
    }
}
