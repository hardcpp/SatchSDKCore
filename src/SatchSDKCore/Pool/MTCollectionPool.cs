using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A Collection such as List, HashSet, Dictionary etc can be pooled and reused by using a CollectionPool.
/// </summary>
public class MTCollectionPool<TCollectionType, TItemType>
    where TCollectionType : class, ICollection<TItemType>, new()
{
    private static readonly MTObjectPool<TCollectionType> s_Pool = new(
        () => new TCollectionType(),
        actionOnRelease: x => x.Clear(),
        defaultCapacity: 100);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple get
    /// </summary>
    /// <returns></returns>
    public static TCollectionType Get()
        => s_Pool.Get();

    /// <summary>
    /// Release an element
    /// </summary>
    /// <param name="p_Element">Element to release</param>
    public static void Release(TCollectionType p_Element)
        => s_Pool.Release(p_Element);
}
