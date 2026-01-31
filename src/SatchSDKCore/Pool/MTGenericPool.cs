namespace SSC.Pool;

/// <summary>
/// Provides a static implementation of Pool.ObjectPool_1.
/// </summary>
public class MTGenericPool<TObjectType>
    where TObjectType : class, new()
{
    private static readonly MTObjectPool<TObjectType> s_Pool = new(() => new());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    public static TObjectType Get()
        => s_Pool.Get();
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public static void Release(TObjectType p_Element)
        => s_Pool.Release(p_Element);
}
