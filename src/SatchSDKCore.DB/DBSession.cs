using System;
using System.Data.Common;

namespace SSC.DB;

/// <summary>
/// DB session object that will handle all read/transformation for a session
/// </summary>
public abstract class DBSession : IDisposable
{
    public abstract DBInstance DBInstance { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    public void Dispose() => Dispose(false);
    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    public abstract void Dispose(bool force = false);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a DbCommand for the driver
    /// </summary>
    /// <returns>New DbCommand</returns>
    public abstract DbCommand CreateDbCommand();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public abstract void Commit();
    public abstract void Rollback();
}
