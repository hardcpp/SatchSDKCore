using Npgsql;
using System;

namespace SSC.DB.Drivers;

/// <summary>
/// PostgreSQL DB Instance implementation
/// </summary>
public class PostgreSqlDbInstance : DbInstance
{
    private readonly PostgreSqlDbSession[] _connections;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override Expressions.IDbQueryDialect DbDialect => new MySqlDbDialect();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Instance name</param>
    /// <param name="hostname">Server host</param>
    /// <param name="port">Server port</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="databaseName">DB name</param>
    /// <param name="poolSize">Pool size</param>
    public PostgreSqlDbInstance(
        string  name,
        string  hostname,
        uint    port,
        string  username,
        string  password,
        string  databaseName,
        uint    poolSize        = 5)
        : base(name)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            KeepAlive = 5, Timeout = 10, MaxPoolSize = 0, Pooling = false,
            Host = hostname,
            Port = (int)port,
            Username = username,
            Password = password,
            Database = databaseName,
            ClientEncoding = "UTF8",
            Encoding = "UTF8"
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        _connections = new PostgreSqlDbSession[poolSize];
        for (var i = 0; i < poolSize; ++i)
            _connections[i] = new PostgreSqlDbSession(this, connectionString, $"{username}:{databaseName}@{hostname}:{port}");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns>First free connection</returns>
    internal override IDbSession AcquireSession()
    {
        while (true)
        {
            for (var i = 0; i < _connections.Length; ++i)
            {
                if (!_connections[i].TryAcquire())
                    continue;

                return _connections[i];
            }

            System.Threading.Thread.Yield();
        }
    }
    /// <summary>
    /// Release a database session
    /// </summary>
    /// <param name="session">Session to release</param>
    internal override void ReleaseSession(IDbSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session is not PostgreSqlDbSession postgreSqlDbSession
            || Array.IndexOf(_connections, postgreSqlDbSession) == -1)
            throw new Exception("The provided session if not from this driver");

        postgreSqlDbSession.Release();

        if (_tlsSession.Value == session)
            _tlsSession.Value = null;
    }
}
