using MySqlConnector;
using System;

namespace SSC.DB.Drivers;

/// <summary>
/// MySQL DB Instance implementation
/// </summary>
public class MySqlDbInstance : DbInstance
{
    private MySqlDbSession[] _connections = Array.Empty<MySqlDbSession>();

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
    public MySqlDbInstance(
        string  name,
        string  hostname,
        uint    port,
        string  username,
        string  password,
        string  databaseName,
        uint    poolSize        = 5)
        : base(name)
    {
        var connectionStringBuilder = new MySqlConnectionStringBuilder
        {
            Keepalive = 5, ConnectionTimeout = 10, MaximumPoolSize = 0, Pooling = false,
            Server = hostname,
            Port = port,
            UserID = username,
            Password = password,
            Database = databaseName,
            CharacterSet = "utf8mb4"
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        _connections = new MySqlDbSession[poolSize];
        for (var i = 0; i < poolSize; ++i)
            _connections[i] = new MySqlDbSession(this, connectionString, $"{username}:{databaseName}@{hostname}:{port}");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns></returns>
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

        if (session is not MySqlDbSession mySqlDbSession
            || Array.IndexOf(_connections, mySqlDbSession) == -1)
            throw new Exception("The provided session if not from this driver");

        mySqlDbSession.Release();

        if (_tlsSession.Value == session)
            _tlsSession.Value = null;
    }
}
