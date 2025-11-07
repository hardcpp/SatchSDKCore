using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace SSC.DB;

/// <summary>
/// Generic database instance
/// </summary>
public abstract class DBInstance
{
    private static DBInstance[] m_Instances = Array.Empty<DBInstance>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    protected ThreadLocal<DBSession?>       m_TLSSession      = new(() => null);
    protected ThreadLocal<StringBuilder>    m_TLSQueryBuilder = new(() => new(2048));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly string                  Name;
    public abstract Expressions.IDBQueryDialect  DBDialect { get; }

    public static DBInstance[] Instances => m_Instances;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create DBInstance
    /// </summary>
    /// <param name="name">Instance name</param>
    /// <param name="driver">Driver type</param>
    /// <param name="hostname">Server host</param>
    /// <param name="port">Server port</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="databaseName">DB name</param>
    /// <param name="poolSize">Pool size</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static DBInstance Create(
        string  name,
        string  driver,
        string  hostname,
        uint    port,
        string  username,
        string  password,
        string  databaseName,
        uint    poolSize        = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(poolSize, 1u, "poolSize");

        var l_Instance = null as DBInstance;
        if (driver.Equals("mysql", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("mariadb", StringComparison.OrdinalIgnoreCase))
            l_Instance = new Drivers.MySQLDBInstance(name, hostname, port, username, password, databaseName, poolSize);
        else if (driver.Equals("postgre", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("postgres", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            l_Instance = new Drivers.PostgreSQLDBInstance(name, hostname, port, username, password, databaseName, poolSize);

        if (l_Instance == null)
            throw new Exception($"Unsupported database driver {driver}");

        Array.Resize(ref m_Instances, m_Instances.Length + 1);
        m_Instances[^1] = l_Instance;

        DBModelMetadata.InitAll();

        return l_Instance;
    }
    /// <summary>
    /// Get DBInstance by name
    /// </summary>
    /// <param name="name">Lookup name</param>
    /// <returns>Found DBInstance</returns>
    /// <exception cref="Exception">If no result found with the lookup name</exception>
    public static DBInstance Get(string name)
    {
        for (var l_I = 0; l_I < m_Instances.Length; l_I++)
        {
            if (m_Instances[l_I].Name != name)
                continue;

            return m_Instances[l_I];
        }

        throw new Exception($"No database instance found with name '{name}'");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Instance name</param>
    protected DBInstance(string name)
    {
        Name = name;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get and wait for a thread local storage session, you will need to dispose the session for later use
    /// or using it in a using scope
    /// </summary>
    /// <returns>DB session</returns>
    public DBSession GetTLSSession(bool skipAcquire = false)
    {
        if (m_TLSSession.Value != null || skipAcquire)
            return m_TLSSession.Value!;

        m_TLSSession.Value = AcquireSession();
        return m_TLSSession.Value;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns></returns>
    internal abstract DBSession AcquireSession();
    /// <summary>
    /// Release a database session
    /// </summary>
    /// <param name="session">Session to release</param>
    internal abstract void ReleaseSession(DBSession session);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get thread local storage query builder
    /// </summary>
    /// <returns></returns>
    internal StringBuilder GetTLSQueryBuilder()
    {
        m_TLSQueryBuilder.Value!.Clear();
        return m_TLSQueryBuilder.Value!;
    }
}
