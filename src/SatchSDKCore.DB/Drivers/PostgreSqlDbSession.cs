using Npgsql;
using System;
using System.Data;
using System.Data.Common;

namespace SSC.DB.Drivers;

/// <summary>
/// PostgreSQL IDbSession implementation
/// </summary>
internal class PostgreSqlDbSession : IDbSession
{
    private readonly object                  _lock                      = new object();
    private readonly PostgreSqlDbInstance    _instance;
    private readonly string                  _connectionString;
    private readonly string                  _logIdentifier;
    private          NpgsqlConnection?       _postgreSqlDbConnection    = null;
    private volatile bool                    _inUse                     = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override DbInstance DbInstance => _instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="postgreSqlDbInstance">Parent PostgreSqlDbInstance</param>
    /// <param name="connectionString">Connection string for the driver</param>
    /// <param name="logIdentifier">Identifier for logs</param>
    internal PostgreSqlDbSession(PostgreSqlDbInstance postgreSqlDbInstance, string connectionString, string logIdentifier)
    {
        _instance          = postgreSqlDbInstance;
        _connectionString  = connectionString;
        _logIdentifier     = logIdentifier;

        OpenDbConnection();

        /*var ee = new PostgreSQLCommand("insert into oc_accounts(uid, data) VALUES (@arg1, @arg2)", _postgreSqlDbConnection);
        ee.Parameters.Add(new PostgreSQLParameter(){ ParameterName = "@uid"});
        ee.Parameters.Add(new PostgreSQLParameter(){ ParameterName = "@data" });
        ee.Prepare();

        var reader = new PostgreSQLCommand("SELECT\r\n    OWNER_OBJECT_TYPE, OWNER_OBJECT_SCHEMA, OWNER_OBJECT_NAME,\r\n    STATEMENT_NAME, SQL_TEXT\r\nFROM performance_schema.`prepared_statements_instances`;", _postgreSqlDbConnection).ExecuteReader();
        while (reader.ReadSocket())
        {
            System.Console.WriteLine(reader.GetValue(0));
            System.Console.WriteLine(reader.GetValue(1));
        }*/
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    /// <param name="force">Force to dispose</param>
    public override void DisposeFinal(bool force = false)
    {
        _instance.ReleaseSession(this);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a DbCommand for the driver
    /// </summary>
    /// <returns>New DbCommand</returns>
    public override DbCommand CreateDbCommand()
    {
        return new NpgsqlCommand(null, _postgreSqlDbConnection, null);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void Commit()
    {
        throw new NotImplementedException();
    }

    public override void Rollback()
    {
        throw new NotImplementedException();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to acquire this IDbSession
    /// </summary>
    /// <returns>True if acquired</returns>
    internal bool TryAcquire()
    {
        if (_inUse)
            return false;

        lock (_lock)
        {
            if (_inUse)
                return false;

            try
            {
                switch (_postgreSqlDbConnection?.State)
                {
                    case ConnectionState.Open:
                    case ConnectionState.Connecting:
                    case ConnectionState.Executing:
                    case ConnectionState.Fetching:
                        break;

                    case ConnectionState.Closed:
                    case ConnectionState.Broken:
                        OpenDbConnection();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch(Exception) { }

            if (_postgreSqlDbConnection?.State != ConnectionState.Open)
                return false;

            _inUse = true;
        }

        return true;
    }
    /// <summary>
    /// Release from a TryAcquire
    /// </summary>
    internal void Release()
    {
        if (_inUse)
            return;

        while (_postgreSqlDbConnection!.State == System.Data.ConnectionState.Fetching
            || _postgreSqlDbConnection!.State == System.Data.ConnectionState.Executing)
            System.Threading.Thread.Yield();

        lock (_lock)
            _inUse = false;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Open this DB Connection
    /// </summary>
    /// <exception cref="Exception">If the connection failed</exception>
    private void OpenDbConnection()
    {
        try
        {
            if (_postgreSqlDbConnection != null)
            {
                try { _postgreSqlDbConnection.Close(); }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception) { }
            }

            _postgreSqlDbConnection = new NpgsqlConnection(_connectionString);
            _postgreSqlDbConnection.Open();
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Database][PostgreSqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");
            Logging.Log(ELogSeverity.Error, exception);

            throw new Exception($"[Database][PostgreSqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");
        }
    }
}
