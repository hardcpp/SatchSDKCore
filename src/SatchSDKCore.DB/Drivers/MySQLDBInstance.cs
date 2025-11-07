using MySqlConnector;
using System;

namespace SSC.DB.Drivers;

/// <summary>
/// MySQL DB Instance implementation
/// </summary>
public class MySQLDBInstance : DBInstance
{
    private MySQLDBSession[] m_Connections = Array.Empty<MySQLDBSession>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override Expressions.IDBQueryDialect DBDialect => new MySqlDBDialect();

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
    public MySQLDBInstance(
        string  name,
        string  hostname,
        uint    port,
        string  username,
        string  password,
        string  databaseName,
        uint    poolSize        = 5)
        : base(name)
    {
        var l_ConnectionStringBuilder = new MySqlConnectionStringBuilder();
        l_ConnectionStringBuilder.Keepalive             = 5;
        l_ConnectionStringBuilder.ConnectionTimeout     = 10;
        l_ConnectionStringBuilder.MaximumPoolSize       = 0;
        l_ConnectionStringBuilder.Pooling               = false;
        l_ConnectionStringBuilder.Server                = hostname;
        l_ConnectionStringBuilder.Port                  = port;
        l_ConnectionStringBuilder.UserID                = username;
        l_ConnectionStringBuilder.Password              = password;
        l_ConnectionStringBuilder.Database              = databaseName;
        l_ConnectionStringBuilder.CharacterSet          = "utf8mb4";

        var l_ConnectionString = l_ConnectionStringBuilder.ConnectionString;

        m_Connections = new MySQLDBSession[poolSize];
        for (var l_I = 0; l_I < poolSize; ++l_I)
            m_Connections[l_I] = new MySQLDBSession(this, l_ConnectionString, $"{username}:{databaseName}@{hostname}:{port}");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns></returns>
    internal override DBSession AcquireSession()
    {
        while (true)
        {
            for (var l_I = 0; l_I < m_Connections.Length; ++l_I)
            {
                if (!m_Connections[l_I].TryAcquire())
                    continue;

                return m_Connections[l_I];
            }

            System.Threading.Thread.Yield();
        }
    }
    /// <summary>
    /// Release a database session
    /// </summary>
    /// <param name="session">Session to release</param>
    internal override void ReleaseSession(DBSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session is not MySQLDBSession l_MySQLDBSession
            || Array.IndexOf(m_Connections, l_MySQLDBSession) == -1)
            throw new Exception("The provided session if not from this driver");

        l_MySQLDBSession.Release();

        if (m_TLSSession.Value == session)
            m_TLSSession.Value = null;
    }
}
