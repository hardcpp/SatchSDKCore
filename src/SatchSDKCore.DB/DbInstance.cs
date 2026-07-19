using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using SSC.DB.Drivers;
using SSC.DB.Expressions;

namespace SSC.DB;

/// <summary>
/// Generic database instance
/// </summary>
public abstract class DbInstance
{
    private static DbInstance[] s_Instances = Array.Empty<DbInstance>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>Stores the session acquired by each thread.</summary>
    protected readonly ThreadLocal<IDbSession?> _tlsSession = new(() => null);
    /// <summary>Stores a reusable query string builder for each thread.</summary>
    protected readonly ThreadLocal<StringBuilder> _tlsQueryBuilder = new(() => new StringBuilder(2048));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets the unique instance name.</summary>
    public readonly string Name;
    /// <summary>Gets the SQL dialect used by this instance.</summary>
    public abstract IDbQueryDialect DbDialect { get; }
    /// <summary>Gets all database instances created in this process.</summary>
    public static DbInstance[] Instances => s_Instances;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Instance name</param>
    protected DbInstance(string name)
    {
        Name = name;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create DbInstance
    /// </summary>
    /// <param name="name">Instance name</param>
    /// <param name="driver">Driver type</param>
    /// <param name="hostname">Server host</param>
    /// <param name="port">Server port</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="databaseName">DB name</param>
    /// <param name="poolSize">Pool size</param>
    /// <returns>The newly registered database instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="poolSize" /> is less than one.</exception>
    /// <exception cref="Exception"><paramref name="driver" /> is not supported.</exception>
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public static DbInstance Create(
        string name,
        string driver,
        string hostname,
        uint port,
        string username,
        string password,
        string databaseName,
        uint poolSize = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(poolSize, 1u);

        var instance = null as DbInstance;
        if (driver.Equals("mysql", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("mariadb", StringComparison.OrdinalIgnoreCase))
        {
            instance = new MySqlDbInstance(name, hostname, port, username, password, databaseName, poolSize);
        }
        else if (driver.Equals("postgre", StringComparison.OrdinalIgnoreCase)
                 || driver.Equals("postgres", StringComparison.OrdinalIgnoreCase)
                 || driver.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            instance = new PostgreSqlDbInstance(name, hostname, port, username, password, databaseName,
                poolSize);
        }

        if (instance == null)
        {
            throw new Exception($"Unsupported database driver {driver}");
        }

        Array.Resize(ref s_Instances, s_Instances.Length + 1);
        s_Instances[^1] = instance;

        DbModelMetadata.InitAll();

        return instance;
    }

    /// <summary>
    /// Get DbInstance by name
    /// </summary>
    /// <param name="name">Lookup name</param>
    /// <returns>Found DbInstance</returns>
    /// <exception cref="Exception">If no result found with the lookup name</exception>
    public static DbInstance Get(string name)
    {
        for (int i = 0; i < s_Instances.Length; i++)
        {
            if (s_Instances[i].Name != name)
            {
                continue;
            }

            return s_Instances[i];
        }

        throw new Exception($"No database instance found with name '{name}'");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get and wait for a thread local storage session, you will need to dispose the session for later use
    /// or using it in a using scope
    /// </summary>
    /// <param name="skipAcquire">When <see langword="true" />, returns only an existing thread-local session.</param>
    /// <returns>DB session</returns>
    public IDbSession GetTlsSession(bool skipAcquire = false)
    {
        if (_tlsSession.Value != null || skipAcquire)
        {
            return _tlsSession.Value!;
        }

        _tlsSession.Value = AcquireSession();
        return _tlsSession.Value;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns>An acquired session owned by this instance.</returns>
    internal abstract IDbSession AcquireSession();

    /// <summary>
    /// Release a database session
    /// </summary>
    /// <param name="session">Session to release</param>
    internal abstract void ReleaseSession(IDbSession session);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get thread local storage query builder
    /// </summary>
    /// <returns>A cleared, thread-local string builder.</returns>
    internal StringBuilder GetTlsQueryBuilder()
    {
        _tlsQueryBuilder.Value!.Clear();
        return _tlsQueryBuilder.Value!;
    }
}
