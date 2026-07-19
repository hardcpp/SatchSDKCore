using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace SSC.DB;

/// <summary>
/// Generic database instance
/// </summary>
public abstract class DbInstance
{
    private static DbInstance[] _instances = Array.Empty<DbInstance>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    protected ThreadLocal<IDbSession?>       _tlsSession      = new(() => null);
    protected ThreadLocal<StringBuilder>    _tlsQueryBuilder = new(() => new(2048));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly string                  Name;
    public abstract Expressions.IDbQueryDialect  DbDialect { get; }

    public static DbInstance[] Instances => _instances;

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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static DbInstance Create(
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

        var instance = null as DbInstance;
        if (driver.Equals("mysql", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("mariadb", StringComparison.OrdinalIgnoreCase))
            instance = new Drivers.MySqlDbInstance(name, hostname, port, username, password, databaseName, poolSize);
        else if (driver.Equals("postgre", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("postgres", StringComparison.OrdinalIgnoreCase)
            || driver.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            instance = new Drivers.PostgreSqlDbInstance(name, hostname, port, username, password, databaseName, poolSize);

        if (instance == null)
            throw new Exception($"Unsupported database driver {driver}");

        Array.Resize(ref _instances, _instances.Length + 1);
        _instances[^1] = instance;

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
        for (var i = 0; i < _instances.Length; i++)
        {
            if (_instances[i].Name != name)
                continue;

            return _instances[i];
        }

        throw new Exception($"No database instance found with name '{name}'");
    }

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
    /// Get and wait for a thread local storage session, you will need to dispose the session for later use
    /// or using it in a using scope
    /// </summary>
    /// <returns>DB session</returns>
    public IDbSession GetTlsSession(bool skipAcquire = false)
    {
        if (_tlsSession.Value != null || skipAcquire)
            return _tlsSession.Value!;

        _tlsSession.Value = AcquireSession();
        return _tlsSession.Value;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Acquire a database session
    /// </summary>
    /// <returns></returns>
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
    /// <returns></returns>
    internal StringBuilder GetTlsQueryBuilder()
    {
        _tlsQueryBuilder.Value!.Clear();
        return _tlsQueryBuilder.Value!;
    }
}
