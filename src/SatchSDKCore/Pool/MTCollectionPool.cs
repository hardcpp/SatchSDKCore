using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A Collection such as List, HashSet, Dictionary etc can be pooled and reused by using a CollectionPool.
/// </summary>
public class MTCollectionPool<t_Collection, t_Item>
    where t_Collection : class, ICollection<t_Item>, new()
{
    private static readonly MTObjectPool<t_Collection> s_Pool = new(() => new(), actionOnRelease: (x => x.Clear()), defaultCapacity: 100);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    public static t_Collection Get()
        => s_Pool.Get();
    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public static void Release(t_Collection p_Element)
        => s_Pool.Release(p_Element);
}