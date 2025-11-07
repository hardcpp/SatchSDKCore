namespace SSC.Pool;

/// <summary>
/// Provides a static implementation of Pool.ObjectPool_1.
/// </summary>
public class MTGenericPool<t_Value>
    where t_Value : class, new()
{
    private static readonly MTObjectPool<t_Value> s_Pool = new(() => new());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    public static t_Value Get()
        => s_Pool.Get();
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public static void Release(t_Value p_Element)
        => s_Pool.Release(p_Element);
}