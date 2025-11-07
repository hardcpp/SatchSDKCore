using Npgsql;
using System;

namespace SSC.DB.Drivers;

/// <summary>
/// PostgreSQL DB Instance implementation
/// </summary>
public class PostgreSQLDBInstance : DBInstance
{
    private PostgreSQLDBSession[] m_Connections = Array.Empty<PostgreSQLDBSession>();

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
    public PostgreSQLDBInstance(
        string  name,
        string  hostname,
        uint    port,
        string  username,
        string  password,
        string  databaseName,
        uint    poolSize        = 5)
        : base(name)
    {
        var l_ConnectionStringBuilder = new NpgsqlConnectionStringBuilder();
        l_ConnectionStringBuilder.KeepAlive             = 5;
        l_ConnectionStringBuilder.Timeout               = 10;
        l_ConnectionStringBuilder.MaxPoolSize           = 0;
        l_ConnectionStringBuilder.Pooling               = false;
        l_ConnectionStringBuilder.Host                  = hostname;
        l_ConnectionStringBuilder.Port                  = (int)port;
        l_ConnectionStringBuilder.Username              = username;
        l_ConnectionStringBuilder.Password              = password;
        l_ConnectionStringBuilder.Database              = databaseName;
        l_ConnectionStringBuilder.ClientEncoding        = "UTF8";
        l_ConnectionStringBuilder.Encoding              = "UTF8";

        var l_ConnectionString = l_ConnectionStringBuilder.ConnectionString;

        m_Connections = new PostgreSQLDBSession[poolSize];
        for (var l_I = 0; l_I < poolSize; ++l_I)
            m_Connections[l_I] = new PostgreSQLDBSession(this, l_ConnectionString, $"{username}:{databaseName}@{hostname}:{port}");
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

        if (session is not PostgreSQLDBSession l_PostgreSQLDBSession
            || Array.IndexOf(m_Connections, l_PostgreSQLDBSession) == -1)
            throw new Exception("The provided session if not from this driver");

        l_PostgreSQLDBSession.Release();

        if (m_TLSSession.Value == session)
            m_TLSSession.Value = null;
    }
}
