using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for Lists.
/// </summary>
public class STListPool<TValueType>
    : STCollectionPool<List<TValueType>, TValueType>
{
}
