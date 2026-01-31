using System;

namespace SSC.Pool;

/// <summary>
/// Guarded pooled object
/// </summary>
/// <typeparam name="t_Type">Type of the elemnt</typeparam>
public class PooledObject<t_Type>
    : IDisposable
    where t_Type : class
{
    private bool _disposed;
    private readonly IObjectPool<t_Type> _pool;
    private readonly t_Type _value;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public t_Type Value
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Value));

            return _value;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pool">Source pool</param>
    /// <param name="element">Element instance to guard</param>
    internal PooledObject(IObjectPool<t_Type> pool, t_Type element)
    {
        _pool = pool;
        _value = element;

        _disposed = false;
    }
    /// <summary>
    /// Destructor
    /// </summary>
    ~PooledObject()
    {
        (this as IDisposable).Dispose();
    }
    /// <summary>
    /// Dispose the object
    /// </summary>
    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
        _pool.Release(_value);
    }
}
