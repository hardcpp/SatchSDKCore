using System;
using System.Data.Common;

namespace SSC.DB;

/// <summary>
/// DB session object that will handle all read/transformation for a session
/// </summary>
public abstract class IDbSession : IDisposable
{
    public abstract DbInstance DbInstance { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    public void Dispose() => DisposeFinal(false);
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

    public abstract void Commit();
    public abstract void Rollback();
}
