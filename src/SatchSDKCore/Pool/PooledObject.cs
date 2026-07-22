using System;

namespace SSC.Pool;

/// <summary>
/// Guarded pooled object
/// </summary>
/// <typeparam name="TObjectType">Type of the elemnt</typeparam>
public class PooledObject<TObjectType>
    : IDisposable
    where TObjectType : class
{
    private readonly IObjectPool<TObjectType> _pool;
    private readonly TObjectType              _value;
    private          bool                     _disposed;


    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public TObjectType Value
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
    internal PooledObject(IObjectPool<TObjectType> pool, TObjectType element)
    {
        _pool  = pool;
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
