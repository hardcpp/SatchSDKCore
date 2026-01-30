using SSC.Pool;

namespace SatchSDKCore.Tests.Pool;

/// <summary>
/// Tests for MTCollectionPool class.
/// </summary>
public class MTCollectionPoolTests
{
    [Fact]
    public void Get_ReturnsNewCollection()
    {
        // Act
        // Get a collection from the pool - should return a new empty List<int>
        var collection = MTCollectionPool<List<int>, int>.Get();

        // Assert
        // The collection should be ready to use
        Assert.NotNull(collection);
        Assert.Empty(collection);
    }

    [Fact]
    public void Get_ReturnsEmptyCollection()
    {
        // Act
        // Collections from the pool should always start empty
        var collection = MTCollectionPool<List<string>, string>.Get();

        // Assert
        Assert.NotNull(collection);
        Assert.Empty(collection);
    }

    [Fact]
    public void Release_ClearsCollection()
    {
        // Arrange
        // Get a collection and add some items to it
        var collection = MTCollectionPool<List<int>, int>.Get();
        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        // Act
        // Release the collection back to the pool
        MTCollectionPool<List<int>, int>.Release(collection);
        // Get a new collection (may be the same instance)
        var newCollection = MTCollectionPool<List<int>, int>.Get();

        // Assert
        // The collection should be cleared when released, so next Get returns empty
        Assert.Empty(newCollection);
    }

    [Fact]
    public void GetAndRelease_ReusesSameInstance()
    {
        // Arrange
        // Get a collection from the pool
        var collection1 = MTCollectionPool<List<int>, int>.Get();
        collection1.Add(42);

        // Act
        // Release it back to the pool and get another one
        MTCollectionPool<List<int>, int>.Release(collection1);
        var collection2 = MTCollectionPool<List<int>, int>.Get();

        // Assert
        // Pool should reuse the same instance for efficiency
        Assert.Same(collection1, collection2);
        Assert.Empty(collection2); // Should be cleared when released
    }

    [Fact]
    public void HashSet_CanBePooled()
    {
        // Act
        // MTCollectionPool is generic and works with any ICollection type, not just List
        var hashSet = MTCollectionPool<HashSet<string>, string>.Get();
        hashSet.Add("test");
        hashSet.Add("value");

        // Assert
        Assert.Equal(2, hashSet.Count);

        // Release and verify it's cleared
        MTCollectionPool<HashSet<string>, string>.Release(hashSet);
        var newHashSet = MTCollectionPool<HashSet<string>, string>.Get();
        Assert.Empty(newHashSet);
    }

    [Fact]
    public void MultipleGetsAndReleases_WorkCorrectly()
    {
        // Arrange & Act
        // Get multiple collections from the pool
        var collections = new List<List<int>>();
        for (int i = 0; i < 10; i++)
        {
            collections.Add(MTCollectionPool<List<int>, int>.Get());
        }

        // All should be different instances initially (pool creates as needed)
        Assert.Equal(10, collections.Distinct().Count());

        // Release all collections back to the pool
        foreach (var collection in collections)
        {
            MTCollectionPool<List<int>, int>.Release(collection);
        }

        // Get 10 collections again - pool should reuse the released instances
        var newCollections = new List<List<int>>();
        for (int i = 0; i < 10; i++)
        {
            newCollections.Add(MTCollectionPool<List<int>, int>.Get());
        }

        // Should have reused at least some instances (memory efficiency)
        var reusedCount = newCollections.Count(nc => collections.Contains(nc));
        Assert.True(reusedCount > 0);
    }
}