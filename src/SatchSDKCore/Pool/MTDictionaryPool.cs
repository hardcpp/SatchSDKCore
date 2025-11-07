using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for Dictionaries.
/// </summary>
public class MTDictionaryPool<t_Key, t_Value>
    : MTCollectionPool<Dictionary<t_Key, t_Value>, KeyValuePair<t_Key, t_Value>>
    where t_Key : notnull
{

}