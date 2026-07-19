using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using SSC.Db.Expressions;

namespace SSC.Db.Querying;

/// <summary>
/// DB query builder
/// </summary>
public class DbQuery
{
    private static readonly ThreadLocal<DbQuery?> s_Instances = new(() => new DbQuery());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly Dictionary<string, int> _parameterNameGenerator = new();
    private DbCommand? _dbCommand;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets the logical query parameter values.</summary>
    public readonly Dictionary<string, object> Parameters = new();
    /// <summary>Gets the mutable SQL text buffer.</summary>
    public readonly StringBuilder Query;

    /// <summary>Gets the database instance associated with the session.</summary>
    public DbInstance DbInstance;
    /// <summary>Gets the session used to execute this query.</summary>
    public IDbSession DbSession;
    /// <summary>Gets the model metadata associated with the query, when applicable.</summary>
    public DbModelMetadata? DbModelMetadata;
    /// <summary>Gets the dialect used to render SQL fragments.</summary>
    public IDbQueryDialect DbQueryDialect;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public DbQuery()
    {
        DbSession = null!;
        DbInstance = null!;
        DbQueryDialect = null!;
        Query = new StringBuilder(2048);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a thread local storage instance
    /// </summary>
    /// <returns>The query object associated with the current thread.</returns>
    public static DbQuery GetTlsSingleton()
        => s_Instances.Value!;

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
        {
            _dbCommand.Cancel();
            _dbCommand.Dispose();
        }

        DbSession = dbSession;
        DbInstance = dbSession.DbInstance;
        DbQueryDialect = dbSession.DbInstance.DbDialect;
        DbModelMetadata = dBModelMetadata;

        _dbCommand = dbSession.CreateDbCommand();
        _parameterNameGenerator.Clear();

        Query.Clear();
        Parameters.Clear();
    }

    /// <summary>
    /// Executes the command and returns the number of affected rows.
    /// </summary>
    /// <returns>The number of rows affected.</returns>
    public int ExecuteNonQuery()
    {
        ArgumentNullException.ThrowIfNull(_dbCommand);

        PrepareCommand();
        return _dbCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the command and returns a reader over its result rows.
    /// </summary>
    /// <returns>A data reader owned by the caller.</returns>
    public DbDataReader ExecuteReader()
    {
        ArgumentNullException.ThrowIfNull(_dbCommand);
        PrepareCommand();
        return _dbCommand.ExecuteReader();
    }

    /// <summary>
    /// Executes the command and returns the first column of its first row.
    /// </summary>
    /// <returns>The scalar value, or <see langword="null" /> when no value is returned.</returns>
    public object? ExecuteScalar()
    {
        ArgumentNullException.ThrowIfNull(_dbCommand);
        PrepareCommand();
        return _dbCommand.ExecuteScalar();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Generate a parameter name with an optional prefix, increment a stored counter
    /// </summary>
    /// <param name="prefix">Optional prefix for the parameter name</param>
    /// <returns>Generated parameter name</returns>
    public string GenerateParameterName(string? prefix)
    {
        prefix ??= string.Empty;

        _parameterNameGenerator.TryAdd(prefix, 0);

        int index = ++_parameterNameGenerator[prefix];

        return $"@{prefix}{index}";
    }

    /// <summary>
    /// Add a parameter
    /// </summary>
    /// <param name="name">Name of the parameter.</param>
    /// <param name="value">Value of the parameter</param>
    /// <exception cref="Exception">If a parameter with the same name already exists</exception>
    public void AddParameter(string name, object? value)
    {
        if (_dbCommand == null)
        {
            throw new Exception("The query builder was not reset!");
        }

        ArgumentNullException.ThrowIfNull(name);

        if (_dbCommand.Parameters.IndexOf(name) != -1)
        {
            throw new Exception($"An argument with the name '{name}' is already existing");
        }

        DbParameter parameter = _dbCommand.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;

        _dbCommand.Parameters.Add(parameter);
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

        Console.WriteLine("\n" + _dbCommand.CommandText + "\n");
    }
}
