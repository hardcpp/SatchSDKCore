using MySqlConnector;
using System;
using System.Data.Common;

namespace SSC.DB.Drivers;

/// <summary>
/// MySql IDbSession implementation
/// </summary>
internal class MySqlDbSession : IDbSession
{
    private readonly object              _lock              = new object();
    private readonly MySqlDbInstance     _instance;
    private readonly string              _connectionString;
    private readonly string              _logIdentifier;
    private          MySqlConnection?    _mySqlDbConnection = null;
    private volatile bool                _inUse             = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override DbInstance DbInstance => _instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mySqlDbInstance">Parent MySqlDBInstance</param>
    /// <param name="connectionString">Connection string for the driver</param>
    /// <param name="logIdentifier">Identifier for logs</param>
    internal MySqlDbSession(MySqlDbInstance mySqlDbInstance, string connectionString, string logIdentifier)
    {
        _instance          = mySqlDbInstance;
        _connectionString  = connectionString;
        _logIdentifier     = logIdentifier;

        OpenDBConnection();
        /*var ee = new MySqlCommand("insert into oc_accounts(uid, data) VALUES (@arg1, @arg2)", _mySqlDbConnection);
        ee.Parameters.Add(new MySqlParameter(){ ParameterName = "@uid"});
        ee.Parameters.Add(new MySqlParameter(){ ParameterName = "@data" });
        ee.Prepare();

        var reader = new MySqlCommand("SELECT\r\n    OWNER_OBJECT_TYPE, OWNER_OBJECT_SCHEMA, OWNER_OBJECT_NAME,\r\n    STATEMENT_NAME, SQL_TEXT\r\nFROM performance_schema.`prepared_statements_instances`;", _mySqlDbConnection).ExecuteReader();
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
        return new MySqlCommand(_mySqlDbConnection, null);
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
                switch (_mySqlDbConnection?.State)
                {
                    case System.Data.ConnectionState.Closed:
                    case System.Data.ConnectionState.Broken:
                        OpenDBConnection();
                        break;

                }
            }
            catch(Exception) { }

            if (_mySqlDbConnection?.State != System.Data.ConnectionState.Open)
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
        if (!_inUse)
            return;

        while (_mySqlDbConnection!.State == System.Data.ConnectionState.Fetching
            || _mySqlDbConnection!.State == System.Data.ConnectionState.Executing)
            System.Threading.Thread.Yield();

        lock (_lock)
            _inUse = false;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Open a DB Connection
    /// </summary>
    /// <param name="connection">Connection to open</param>
    /// <exception cref="Exception"></exception>
    private void OpenDBConnection()
    {
        try
        {
            if (_mySqlDbConnection != null)
            {
                try { _mySqlDbConnection.Close(); }
                catch (Exception) { }
            }

            _mySqlDbConnection = new MySqlConnection(_connectionString);
            _mySqlDbConnection.Open();

            MySqlCommand l_Query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", _mySqlDbConnection);
            l_Query.ExecuteNonQuery();
        }
        catch (MySqlException l_Exception)
        {
            switch (l_Exception.Number)
            {
                case 0:
                    Logging.Log(ELogSeverity.Error, $"[Database][MySqlDbSession.OpenDBConnection<{_logIdentifier}>] Can not connect to the database server");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    throw new Exception($"[Database][MySqlDbSession.OpenDBConnection<{_logIdentifier}>] Can not connect to the database server");

                case 1045:
                case 1042:
                    Logging.Log(ELogSeverity.Error, $"[Database][MySqlDbSession.OpenDBConnection<{_logIdentifier}>] Authentification failed");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    throw new Exception($"[Database][MySqlDbSession.OpenDBConnection<{_logIdentifier}>] Authentification failed");

            }
        }
    }
}
