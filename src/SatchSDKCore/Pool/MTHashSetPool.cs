using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for HashSets.
/// </summary>
public class MTHashSetPool<t_Value>
    : MTCollectionPool<HashSet<t_Value>, t_Value>
{

}