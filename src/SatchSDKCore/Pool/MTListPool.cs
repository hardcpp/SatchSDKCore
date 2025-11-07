using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for Lists.
/// </summary>
public class MTListPool<t_Value>
    : MTCollectionPool<List<t_Value>, t_Value>
{

}