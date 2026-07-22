using System;
using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A stack based Pool.IObjectPool_1.
/// </summary>
public class STObjectPool<TObjectType>
    : IDisposable, IObjectPool<TObjectType>
    where TObjectType : class
{
    private readonly Action<TObjectType>? _actionOnDestroy;
    private readonly Action<TObjectType>? _actionOnGet;
    private readonly Action<TObjectType>? _actionOnRelease;
    private readonly bool                 _collectionCheck;
    private readonly Func<TObjectType>    _createFunc;
    private readonly int                  _maxSize;
    private readonly Stack<TObjectType>   _stack;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public int CountAll      { get; private set; }
    public int CountActive   => CountAll - CountInactive;
    public int CountInactive => _stack.Count;


    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public STObjectPool(
        Func<TObjectType>    createFunc,
        Action<TObjectType>? actionOnGet     = null,
        Action<TObjectType>? actionOnRelease = null,
        Action<TObjectType>? actionOnDestroy = null,
        bool                 collectionCheck = true,
        int                  defaultCapacity = 10,
        int                  maxSize         = 100)
    {
        if (createFunc == null)
            throw new ArgumentNullException(nameof(createFunc));

        if (maxSize <= 0)
            throw new ArgumentException("Max Size must be greater than 0", nameof(maxSize));

        _stack           = new Stack<TObjectType>(defaultCapacity);
        _createFunc      = createFunc;
        _maxSize         = maxSize;
        _actionOnGet     = actionOnGet;
        _actionOnRelease = actionOnRelease;
        _actionOnDestroy = actionOnDestroy;
        _collectionCheck = collectionCheck;

        while (defaultCapacity-- > 0)
        {
            _stack.Push(_createFunc());
            CountAll++;
        }
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
    public TObjectType Get()
    {
        TObjectType result;

        if (_stack.Count == 0)
        {
            result = _createFunc();
            CountAll++;
        }
        else
            result = _stack.Pop();

        _actionOnGet?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Managed object get
    /// </summary>
    /// <param name="p_Element">Result value</param>
    /// <returns></returns>
    public PooledObject<TObjectType> Get(out TObjectType p_Element)
        => new(this, p_Element = Get());

    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public void Release(TObjectType p_Element)
    {
        if (_collectionCheck && _stack.Count > 0 && _stack.Contains(p_Element))
        {
            throw new InvalidOperationException(
                "Trying to release an object that has already been released to the pool.");
        }

        _actionOnRelease?.Invoke(p_Element);

        if (CountInactive < _maxSize)
            _stack.Push(p_Element);
        else
            _actionOnDestroy?.Invoke(p_Element);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Clear the object pool
    /// </summary>
    public void Clear()
    {
        if (_actionOnDestroy != null)
        {
            foreach (TObjectType current in _stack)
                _actionOnDestroy(current);
        }

        _stack.Clear();
        CountAll = 0;
    }
}
