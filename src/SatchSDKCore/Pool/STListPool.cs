using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for Lists.
/// </summary>
public class STListPool<t_Value>
    : STCollectionPool<List<t_Value>, t_Value>
{

}