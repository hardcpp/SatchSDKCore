namespace SSC.Pool;

/// <summary>
/// Object pool interface
/// </summary>
/// <typeparam name="t_Type">Pooled object type</typeparam>
public interface IObjectPool<t_Type>
    where t_Type : class
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
    t_Type Get();
    /// <summary>
    /// Managed object get
    /// </summary>
    /// <param name="p_Element">Result value</param>
    /// <returns></returns>
    PooledObject<t_Type> Get(out t_Type p_Element);
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    void Release(t_Type p_Element);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Clear the object pool
    /// </summary>
    void Clear();
}
