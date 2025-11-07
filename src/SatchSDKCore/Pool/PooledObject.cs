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
    private bool m_Disposed;
    private readonly IObjectPool<t_Type> m_Pool;
    private readonly t_Type m_Value;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public t_Type Value
    {
        get
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Value));

            return m_Value;
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
        m_Pool = pool;
        m_Value = element;

        m_Disposed = false;
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
        if (m_Disposed)
            return;

        m_Pool.Release(m_Value);
    }
}