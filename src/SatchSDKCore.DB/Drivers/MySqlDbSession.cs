using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using MySqlConnector;

namespace SSC.DB.Drivers;

/// <summary>
/// MySql IDbSession implementation
/// </summary>
internal class MySqlDbSession : IDbSession
{
    private readonly string _connectionString;
    private readonly MySqlDbInstance _instance;
    private readonly object _lock = new();
    private readonly string _logIdentifier;
    private volatile bool _inUse;
    private MySqlConnection? _mySqlDbConnection;

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
        _instance = mySqlDbInstance;
        _connectionString = connectionString;
        _logIdentifier = logIdentifier;

        OpenDbConnection();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override DbInstance DbInstance => _instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override void DisposeFinal(bool force = false) => _instance.ReleaseSession(this);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override DbCommand CreateDbCommand() => new MySqlCommand(_mySqlDbConnection, null);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override void Commit() => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Rollback() => throw new NotImplementedException();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to acquire this IDbSession
    /// </summary>
    /// <returns>True if acquired</returns>
    internal bool TryAcquire()
    {
        if (_inUse)
        {
            return false;
        }

        lock (_lock)
        {
            if (_inUse)
            {
                return false;
            }

            try
            {
                switch (_mySqlDbConnection?.State)
                {
                    case ConnectionState.Closed:
                    case ConnectionState.Broken:
                        OpenDbConnection();
                        break;
                }
            }
            catch (Exception)
            {
            }

            if (_mySqlDbConnection?.State != ConnectionState.Open)
            {
                return false;
            }

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
        {
            return;
        }

        while (_mySqlDbConnection!.State == ConnectionState.Fetching
               || _mySqlDbConnection!.State == ConnectionState.Executing)
        {
            Thread.Yield();
        }

        lock (_lock)
        {
            _inUse = false;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Open a DB Connection
    /// </summary>
    /// <exception cref="Exception">The connection cannot be opened or authentication fails.</exception>
    private void OpenDbConnection()
    {
        try
        {
            if (_mySqlDbConnection != null)
            {
                try
                {
                    _mySqlDbConnection.Close();
                }
                catch (Exception)
                {
                }
            }

            _mySqlDbConnection = new MySqlConnection(_connectionString);
            _mySqlDbConnection.Open();

            var query = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999",
                _mySqlDbConnection);
            query.ExecuteNonQuery();
        }
        catch (MySqlException exception)
        {
            switch (exception.Number)
            {
                case 0:
                    Logging.Log(ELogSeverity.Error,
                        $"[Database][MySqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");
                    Logging.Log(ELogSeverity.Error, exception);

                    throw new Exception(
                        $"[Database][MySqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");

                case 1045:
                case 1042:
                    Logging.Log(ELogSeverity.Error,
                        $"[Database][MySqlDbSession.OpenDbConnection<{_logIdentifier}>] Authentification failed");
                    Logging.Log(ELogSeverity.Error, exception);

                    throw new Exception(
                        $"[Database][MySqlDbSession.OpenDbConnection<{_logIdentifier}>] Authentification failed");
            }
        }
    }
}
