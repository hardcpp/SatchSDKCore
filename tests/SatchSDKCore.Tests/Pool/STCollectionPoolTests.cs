using SSC.Pool;

namespace SatchSDKCore.Tests.Pool;

/// <summary>
/// Tests for STCollectionPool (Single-Threaded Collection Pool) static class.
/// </summary>
public class STCollectionPoolTests
{
    [Fact]
    public void Get_ReturnsNewCollection()
    {
        // Act
        // Get a collection from the static pool - should return a new empty List<int>
        var collection = STCollectionPool<List<int>, int>.Get();

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
        var collection = STCollectionPool<List<string>, string>.Get();

        // Assert
        Assert.NotNull(collection);
        Assert.Empty(collection);
    }

    [Fact]
    public void Release_ClearsCollection()
    {
        // Arrange
        // Get a collection and add some items to it
        var collection = STCollectionPool<List<int>, int>.Get();
        collection.Add(1);
        collection.Add(2);
        collection.Add(3);

        // Act
        // Release the collection back to the pool
        STCollectionPool<List<int>, int>.Release(collection);
        // Get a new collection (may be the same instance)
        var newCollection = STCollectionPool<List<int>, int>.Get();

        // Assert
        // The collection should be cleared when released, so next Get returns empty
        Assert.Empty(newCollection);
    }

    [Fact]
    public void GetAndRelease_ReusesSameInstance()
    {
        // Arrange
        // Get a collection from the pool
        var collection1 = STCollectionPool<List<int>, int>.Get();
        collection1.Add(42);

        // Act
        // Release it back to the pool and get another one
        STCollectionPool<List<int>, int>.Release(collection1);
        var collection2 = STCollectionPool<List<int>, int>.Get();

        // Assert
        // Pool should reuse the same instance for efficiency
        Assert.Same(collection1, collection2);
        Assert.Empty(collection2); // Should be cleared when released
    }

    [Fact]
    public void HashSet_CanBePooled()
    {
        // Act
        // STCollectionPool is generic and works with any ICollection type, not just List
        var hashSet = STCollectionPool<HashSet<string>, string>.Get();
        hashSet.Add("test");
        hashSet.Add("value");

        // Assert
        Assert.Equal(2, hashSet.Count);

        // Release and verify it's cleared
        STCollectionPool<HashSet<string>, string>.Release(hashSet);
        var newHashSet = STCollectionPool<HashSet<string>, string>.Get();
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
            collections.Add(STCollectionPool<List<int>, int>.Get());
        }

        // All should be different instances initially (pool creates as needed)
        Assert.Equal(10, collections.Distinct().Count());

        // Release all collections back to the pool
        foreach (var collection in collections)
        {
            STCollectionPool<List<int>, int>.Release(collection);
        }

        // Get 10 collections again - pool should reuse the released instances
        var newCollections = new List<List<int>>();
        for (int i = 0; i < 10; i++)
        {
            newCollections.Add(STCollectionPool<List<int>, int>.Get());
        }

        // Should have reused at least some instances (memory efficiency)
        var reusedCount = newCollections.Count(nc => collections.Contains(nc));
        Assert.True(reusedCount > 0);

        // Cleanup
        foreach (var collection in newCollections)
        {
            STCollectionPool<List<int>, int>.Release(collection);
        }
    }

    [Fact]
    public void Release_WithPopulatedCollection_ClearsBeforeReuse()
    {
        // Arrange
        var collection = STCollectionPool<List<string>, string>.Get();
        collection.Add("item1");
        collection.Add("item2");
        collection.Add("item3");
        Assert.Equal(3, collection.Count);

        // Act
        STCollectionPool<List<string>, string>.Release(collection);
        var reusedCollection = STCollectionPool<List<string>, string>.Get();

        // Assert
        Assert.Same(collection, reusedCollection);
        Assert.Empty(reusedCollection);
    }

    [Fact]
    public void MultipleTypes_UseDifferentPools()
    {
        // Act
        // Different collection types should use different static pool instances
        var listInt = STCollectionPool<List<int>, int>.Get();
        var listString = STCollectionPool<List<string>, string>.Get();
        var hashSetInt = STCollectionPool<HashSet<int>, int>.Get();

        // Assert
        Assert.NotNull(listInt);
        Assert.NotNull(listString);
        Assert.NotNull(hashSetInt);
        Assert.IsType<List<int>>(listInt);
        Assert.IsType<List<string>>(listString);
        Assert.IsType<HashSet<int>>(hashSetInt);

        // Cleanup
        STCollectionPool<List<int>, int>.Release(listInt);
        STCollectionPool<List<string>, string>.Release(listString);
        STCollectionPool<HashSet<int>, int>.Release(hashSetInt);
    }


    [Fact]
    public void HighVolume_GetAndRelease_WorksCorrectly()
    {
        // Arrange
        var iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var collection = STCollectionPool<List<int>, int>.Get();
            collection.Add(i);
            collection.Add(i * 2);
            STCollectionPool<List<int>, int>.Release(collection);
        }

        // Assert
        // Should complete without exceptions
        var finalCollection = STCollectionPool<List<int>, int>.Get();
        Assert.Empty(finalCollection);

        // Cleanup
        STCollectionPool<List<int>, int>.Release(finalCollection);
    }

    [Fact]
    public void Get_AfterRelease_ReturnsCleanCollection()
    {
        // Arrange
        var collection1 = STCollectionPool<HashSet<int>, int>.Get();
        collection1.Add(100);
        collection1.Add(200);
        collection1.Add(300);

        // Act
        STCollectionPool<HashSet<int>, int>.Release(collection1);
        var collection2 = STCollectionPool<HashSet<int>, int>.Get();

        // Assert
        Assert.Same(collection1, collection2);
        Assert.Empty(collection2);
        Assert.DoesNotContain(100, collection2);
        Assert.DoesNotContain(200, collection2);
        Assert.DoesNotContain(300, collection2);

        // Cleanup
        STCollectionPool<HashSet<int>, int>.Release(collection2);
    }

    [Fact]
    public void MultipleGets_WithoutRelease_ReturnDifferentInstances()
    {
        // Act
        // Get multiple collections without releasing them
        var collection1 = STCollectionPool<List<int>, int>.Get();
        var collection2 = STCollectionPool<List<int>, int>.Get();
        var collection3 = STCollectionPool<List<int>, int>.Get();

        // Assert
        // Each Get should return a different instance
        Assert.NotSame(collection1, collection2);
        Assert.NotSame(collection2, collection3);
        Assert.NotSame(collection1, collection3);

        // Cleanup
        STCollectionPool<List<int>, int>.Release(collection1);
        STCollectionPool<List<int>, int>.Release(collection2);
        STCollectionPool<List<int>, int>.Release(collection3);
    }

    [Fact]
    public void Collection_WithComplexObjects_ClearsCorrectly()
    {
        // Arrange
        var collection = STCollectionPool<List<ComplexObject>, ComplexObject>.Get();
        collection.Add(new ComplexObject { Id = 1, Name = "First" });
        collection.Add(new ComplexObject { Id = 2, Name = "Second" });

        // Act
        STCollectionPool<List<ComplexObject>, ComplexObject>.Release(collection);
        var newCollection = STCollectionPool<List<ComplexObject>, ComplexObject>.Get();

        // Assert
        Assert.Same(collection, newCollection);
        Assert.Empty(newCollection);

        // Cleanup
        STCollectionPool<List<ComplexObject>, ComplexObject>.Release(newCollection);
    }

    private class ComplexObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void SortedSet_CanBePooled()
    {
        // Act
        // Test with SortedSet<T> which implements ICollection<T>
        var sortedSet = STCollectionPool<SortedSet<int>, int>.Get();
        sortedSet.Add(3);
        sortedSet.Add(1);
        sortedSet.Add(2);

        // Assert
        Assert.Equal(3, sortedSet.Count);
        Assert.Equal(new[] { 1, 2, 3 }, sortedSet);

        // Release and verify it's cleared
        STCollectionPool<SortedSet<int>, int>.Release(sortedSet);
        var newSortedSet = STCollectionPool<SortedSet<int>, int>.Get();
        Assert.Empty(newSortedSet);

        // Cleanup
        STCollectionPool<SortedSet<int>, int>.Release(newSortedSet);
    }

    [Fact]
    public void SequentialGetRelease_MaintainsPoolEfficiency()
    {
        // Arrange
        var firstCollection = STCollectionPool<List<int>, int>.Get();

        // Act & Assert
        // Repeatedly get and release - should reuse the same instance
        for (int i = 0; i < 10; i++)
        {
            firstCollection.Add(i);
            Assert.Single(firstCollection);

            STCollectionPool<List<int>, int>.Release(firstCollection);
            firstCollection = STCollectionPool<List<int>, int>.Get();

            Assert.Empty(firstCollection);
        }

        // Cleanup
        STCollectionPool<List<int>, int>.Release(firstCollection);
    }

    [Fact]
    public void EmptyCollection_CanBeReleasedAndReused()
    {
        // Arrange
        var collection = STCollectionPool<List<int>, int>.Get();
        Assert.Empty(collection);

        // Act
        // Release an already empty collection
        STCollectionPool<List<int>, int>.Release(collection);
        var newCollection = STCollectionPool<List<int>, int>.Get();

        // Assert
        Assert.Same(collection, newCollection);
        Assert.Empty(newCollection);

        // Cleanup
        STCollectionPool<List<int>, int>.Release(newCollection);
    }
}