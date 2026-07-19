using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using Npgsql;

namespace SSC.DB.Drivers;

/// <summary>
/// PostgreSQL IDbSession implementation
/// </summary>
internal class PostgreSqlDbSession : IDbSession
{
    private readonly string _connectionString;
    private readonly PostgreSqlDbInstance _instance;
    private readonly object _lock = new();
    private readonly string _logIdentifier;
    private volatile bool _inUse;
    private NpgsqlConnection? _postgreSqlDbConnection;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="postgreSqlDbInstance">Parent PostgreSqlDbInstance</param>
    /// <param name="connectionString">Connection string for the driver</param>
    /// <param name="logIdentifier">Identifier for logs</param>
    internal PostgreSqlDbSession(PostgreSqlDbInstance postgreSqlDbInstance, string connectionString,
        string logIdentifier)
    {
        _instance = postgreSqlDbInstance;
        _connectionString = connectionString;
        _logIdentifier = logIdentifier;

        OpenDbConnection();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override DbInstance DbInstance => _instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override void DisposeFinal(bool force = false) => _instance.ReleaseSession(this);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public override DbCommand CreateDbCommand() => new NpgsqlCommand(null, _postgreSqlDbConnection, null);

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
            catch (Exception)
            {
            }

            if (_postgreSqlDbConnection?.State != ConnectionState.Open)
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

        while (_postgreSqlDbConnection!.State == ConnectionState.Fetching
               || _postgreSqlDbConnection!.State == ConnectionState.Executing)
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
    /// Open this DB Connection
    /// </summary>
    /// <exception cref="Exception">If the connection failed</exception>
    private void OpenDbConnection()
    {
        try
        {
            if (_postgreSqlDbConnection != null)
            {
                try
                {
                    _postgreSqlDbConnection.Close();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
            }

            _postgreSqlDbConnection = new NpgsqlConnection(_connectionString);
            _postgreSqlDbConnection.Open();
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error,
                $"[Database][PostgreSqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");
            Logging.Log(ELogSeverity.Error, exception);

            throw new Exception(
                $"[Database][PostgreSqlDbSession.OpenDbConnection<{_logIdentifier}>] Can not connect to the database server");
        }
    }
}
