using System.Collections.Generic;

namespace SSC.Pool;

/// <summary>
/// A version of Pool.CollectionPool_2 for Dictionaries.
/// </summary>
public class MTDictionaryPool<TKeyType, TValueType>
    : MTCollectionPool<Dictionary<TKeyType, TValueType>, KeyValuePair<TKeyType, TValueType>>
    where TKeyType : notnull
{

}
