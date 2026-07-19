using System;
using System.Data.Common;

namespace SSC.DB;

/// <summary>
/// DB session object that will handle all read/transformation for a session
/// </summary>
public abstract class IDbSession : IDisposable
{
    /// <summary>Gets the database instance that owns this session.</summary>
    public abstract DbInstance DbInstance { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    public void Dispose() => DisposeFinal();

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    /// <param name="force">Force to dispose</param>
    public abstract void DisposeFinal(bool force = false);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a DbCommand for the driver
    /// </summary>
    /// <returns>New DbCommand</returns>
    public abstract DbCommand CreateDbCommand();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>Commits the current transaction.</summary>
    public abstract void Commit();

    /// <summary>Rolls back the current transaction.</summary>
    public abstract void Rollback();
}
