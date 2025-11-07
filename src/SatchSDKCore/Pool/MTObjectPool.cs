using System;
using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A stack based Pool.IObjectPool_1.
/// </summary>
public class MTObjectPool<t_Type>
    : IDisposable, IObjectPool<t_Type>
    where t_Type : class
{
    public int CountAll { get; private set; }
    public int CountActive => CountAll - CountInactive;
    public int CountInactive => m_Stack.Count;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly Stack<t_Type> m_Stack;
    private readonly Func<t_Type> m_CreateFunc;
    private readonly Action<t_Type>? m_ActionOnGet;
    private readonly Action<t_Type>? m_ActionOnRelease;
    private readonly Action<t_Type>? m_ActionOnDestroy;
    private readonly int m_MaxSize;
    private readonly bool m_CollectionCheck;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public MTObjectPool(
        Func<t_Type> createFunc,
        Action<t_Type>? actionOnGet = null,
        Action<t_Type>? actionOnRelease = null,
        Action<t_Type>? actionOnDestroy = null,
        bool collectionCheck = true,
        int defaultCapacity = 10,
        int maxSize = 100)
    {
        if (createFunc == null)
            throw new ArgumentNullException(nameof(createFunc));

        if (maxSize <= 0)
            throw new ArgumentException("Max Size must be greater than 0", nameof(maxSize));

        m_Stack = new Stack<t_Type>(defaultCapacity);
        m_CreateFunc = createFunc;
        m_MaxSize = maxSize;
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
        m_ActionOnDestroy = actionOnDestroy;
        m_CollectionCheck = collectionCheck;

        while (defaultCapacity-- > 0)
            m_Stack.Push(m_CreateFunc());
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Dispose the object
    /// </summary>
    public void Dispose()
        => Clear();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    public t_Type Get()
    {
        t_Type l_Result;

        lock (m_Stack)
        {
            if (m_Stack.Count == 0)
            {
                l_Result = m_CreateFunc();
                CountAll++;
            }
            else
                l_Result = m_Stack.Pop();
        }

        m_ActionOnGet?.Invoke(l_Result);
        return l_Result;
    }
    /// <summary>
    /// Managed object get
    /// </summary>
    /// <param name="p_Element">Result value</param>
    /// <returns></returns>
    public PooledObject<t_Type> Get(out t_Type p_Element)
        => new PooledObject<t_Type>(this, p_Element = Get());
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public void Release(t_Type p_Element)
    {
        lock (m_Stack)
        {
            if (m_CollectionCheck && m_Stack.Count > 0 && m_Stack.Contains(p_Element))
                throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");

            m_ActionOnRelease?.Invoke(p_Element);

            if (CountInactive < m_MaxSize)
                m_Stack.Push(p_Element);
            else
                m_ActionOnDestroy?.Invoke(p_Element);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Clear the object pool
    /// </summary>
    public void Clear()
    {
        lock (m_Stack)
        {
            if (m_ActionOnDestroy != null)
            {
                foreach (t_Type l_Current in m_Stack)
                    m_ActionOnDestroy(l_Current);
            }

            m_Stack.Clear();
        }

        CountAll = 0;
    }
}