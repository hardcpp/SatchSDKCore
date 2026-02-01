namespace SSC.Pool;

/// <summary>
/// Object pool interface
/// </summary>
/// <typeparam name="TObjectType">Pooled object type</typeparam>
public interface IObjectPool<TObjectType>
    where TObjectType : class
{
    int CountAll { get; }
    int CountActive { get; }
    int CountInactive { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    TObjectType Get();
    /// <summary>
    /// Managed object get
    /// </summary>
    /// <param name="p_Element">Result value</param>
    /// <returns></returns>
    PooledObject<TObjectType> Get(out TObjectType p_Element);
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    void Release(TObjectType p_Element);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Clear the object pool
    /// </summary>
    void Clear();
}
