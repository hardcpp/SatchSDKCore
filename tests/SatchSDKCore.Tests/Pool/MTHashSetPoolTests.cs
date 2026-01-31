using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for MTHashSetPool class.
/// </summary>
public class MTHashSetPoolTests
{
    [Fact]
    public void Get_ReturnsEmptyHashSet()
    {
        // Act
        // Get a HashSet<int> from the pool - specialized pool for HashSet type
        var hashSet = MTHashSetPool<int>.Get();

        // Assert
        // Should return a ready-to-use empty HashSet
        Assert.NotNull(hashSet);
        Assert.Empty(hashSet);
        Assert.IsType<HashSet<int>>(hashSet);
    }

    [Fact]
    public void Release_ClearsHashSet()
    {
        // Arrange
        // Get a HashSet and add some items to it
        var hashSet = MTHashSetPool<string>.Get();
        hashSet.Add("item1");
        hashSet.Add("item2");
        hashSet.Add("item3");

        // Act
        // Release clears all items before returning to the pool
        MTHashSetPool<string>.Release(hashSet);
        var newHashSet = MTHashSetPool<string>.Get();

        // Assert
        // Next Get should return a clean, empty HashSet
        Assert.Empty(newHashSet);
    }

    [Fact]
    public void GetAndRelease_ReusesHashSetInstance()
    {
        // Arrange
        var hashSet1 = MTHashSetPool<int>.Get();
        hashSet1.Add(1);
        hashSet1.Add(2);

        // Act
        // Release the HashSet back to the pool, then get another
        MTHashSetPool<int>.Release(hashSet1);
        var hashSet2 = MTHashSetPool<int>.Get();

        // Assert
        // Pool should return the same instance to reduce allocations
        Assert.Same(hashSet1, hashSet2);
        Assert.Empty(hashSet2);
    }

    [Fact]
    public void HashSetOperations_WorkNormally()
    {
        // Arrange
        // Pooled HashSets should behave exactly like regular HashSet<T>
        var hashSet = MTHashSetPool<int>.Get();

        // Act & Assert
        // All standard HashSet operations should work as expected
        Assert.True(hashSet.Add(1));
        Assert.Single(hashSet);
        Assert.Contains(1, hashSet);

        Assert.True(hashSet.Add(2));
        Assert.False(hashSet.Add(1)); // Duplicate - HashSet maintains uniqueness
        Assert.Equal(2, hashSet.Count);

        hashSet.UnionWith(new[] { 3, 4, 5 });
        Assert.Equal(5, hashSet.Count);

        hashSet.Remove(3);
        Assert.Equal(4, hashSet.Count);
        Assert.DoesNotContain(3, hashSet);

        hashSet.Clear();
        Assert.Empty(hashSet);

        // Release back to pool when done
        MTHashSetPool<int>.Release(hashSet);
    }

    [Fact]
    public void MultipleTypes_CanBePooledIndependently()
    {
        // Act
        // Each generic type parameter has its own independent pool
        var intHashSet = MTHashSetPool<int>.Get();
        var stringHashSet = MTHashSetPool<string>.Get();
        var doubleHashSet = MTHashSetPool<double>.Get();

        // Assert
        Assert.NotNull(intHashSet);
        Assert.NotNull(stringHashSet);
        Assert.NotNull(doubleHashSet);

        // Verify they're different types - each pool manages its own type
        Assert.IsType<HashSet<int>>(intHashSet);
        Assert.IsType<HashSet<string>>(stringHashSet);
        Assert.IsType<HashSet<double>>(doubleHashSet);
    }

    [Fact]
    public void HashSet_MaintainsUniqueness()
    {
        // Arrange
        // Verify that pooled HashSets maintain the uniqueness constraint
        var hashSet = MTHashSetPool<string>.Get();

        // Act
        hashSet.Add("apple");
        hashSet.Add("banana");
        hashSet.Add("apple"); // Duplicate - should be ignored

        // Assert
        // HashSet should only contain 2 items (duplicates are not added)
        Assert.Equal(2, hashSet.Count);
        Assert.Contains("apple", hashSet);
        Assert.Contains("banana", hashSet);

        // Cleanup
        MTHashSetPool<string>.Release(hashSet);
    }

    [Fact]
    public void SetOperations_WorkCorrectly()
    {
        // Arrange
        // Test mathematical set operations on pooled HashSets
        var hashSet1 = MTHashSetPool<int>.Get();
        hashSet1.UnionWith(new[] { 1, 2, 3 });

        var hashSet2 = new HashSet<int> { 2, 3, 4 };

        // Act - Intersection keeps only common elements
        hashSet1.IntersectWith(hashSet2);

        // Assert
        // Result should be {2, 3} - the intersection of {1,2,3} and {2,3,4}
        Assert.Equal(2, hashSet1.Count);
        Assert.Contains(2, hashSet1);
        Assert.Contains(3, hashSet1);

        // Cleanup
        MTHashSetPool<int>.Release(hashSet1);
    }

    [Fact]
    public void ComplexTypes_CanBePooled()
    {
        // Arrange
        // Pool works with any type, including tuples and custom objects
        var hashSet = MTHashSetPool<(int Id, string Name)>.Get();

        // Act
        hashSet.Add((1, "Alice"));
        hashSet.Add((2, "Bob"));
        hashSet.Add((1, "Alice")); // Duplicate - tuples use value equality

        // Assert
        // Should only contain 2 unique tuples
        Assert.Equal(2, hashSet.Count);

        // Cleanup
        MTHashSetPool<(int, string)>.Release(hashSet);
    }

    [Fact]
    public void IsSubsetOf_WorksCorrectly()
    {
        // Arrange
        // Test subset/superset operations with pooled HashSets
        var hashSet = MTHashSetPool<int>.Get();
        hashSet.UnionWith(new[] { 1, 2 });

        var superSet = new HashSet<int> { 1, 2, 3, 4 };

        // Act & Assert
        // {1, 2} is a subset of {1, 2, 3, 4}
        Assert.True(hashSet.IsSubsetOf(superSet));
        // {1, 2} is NOT a superset of {1, 2, 3, 4}
        Assert.False(hashSet.IsSupersetOf(superSet));

        // Cleanup
        MTHashSetPool<int>.Release(hashSet);
    }
}