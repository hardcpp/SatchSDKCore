using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for MTListPool class.
/// </summary>
public class MTListPoolTests
{
    [Fact]
    public void Get_ReturnsEmptyList()
    {
        // Act
        // Get a List<int> from the pool - specialized pool for List type
        var list = MTListPool<int>.Get();

        // Assert
        // Should return a ready-to-use empty list
        Assert.NotNull(list);
        Assert.Empty(list);
        Assert.IsType<List<int>>(list);
    }

    [Fact]
    public void Release_ClearsList()
    {
        // Arrange
        // Get a list and populate it with some data
        var list = MTListPool<string>.Get();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        // Act
        // Release clears the list before returning it to the pool
        MTListPool<string>.Release(list);
        var newList = MTListPool<string>.Get();

        // Assert
        // Next Get should return a clean, empty list
        Assert.Empty(newList);
    }

    [Fact]
    public void GetAndRelease_ReusesListInstance()
    {
        // Arrange
        var list1 = MTListPool<int>.Get();
        list1.Add(1);
        list1.Add(2);

        // Act
        // Release the list back to the pool, then get another
        MTListPool<int>.Release(list1);
        var list2 = MTListPool<int>.Get();

        // Assert
        // Pool should return the same instance to reduce allocations
        Assert.Same(list1, list2);
        Assert.Empty(list2);
    }

    [Fact]
    public void MultipleTypesCanBePooledIndependently()
    {
        // Act
        // Each generic type parameter has its own independent pool
        var intList = MTListPool<int>.Get();
        var stringList = MTListPool<string>.Get();
        var doubleList = MTListPool<double>.Get();

        // Assert
        Assert.NotNull(intList);
        Assert.NotNull(stringList);
        Assert.NotNull(doubleList);

        // Verify they're different types - each pool manages its own type
        Assert.IsType<List<int>>(intList);
        Assert.IsType<List<string>>(stringList);
        Assert.IsType<List<double>>(doubleList);
    }

    [Fact]
    public void ListOperations_WorkNormally()
    {
        // Arrange
        // Pooled lists should behave exactly like regular List<T>
        var list = MTListPool<int>.Get();

        // Act & Assert
        // All standard List operations should work as expected
        list.Add(1);
        Assert.Single(list);
        Assert.Equal(1, list[0]);

        list.AddRange(new[] { 2, 3, 4 });
        Assert.Equal(4, list.Count);

        list.Remove(2);
        Assert.Equal(3, list.Count);

        list.Clear();
        Assert.Empty(list);

        // Release back to pool when done
        MTListPool<int>.Release(list);
    }

    [Fact]
    public void ComplexTypes_CanBePooled()
    {
        // Arrange
        // Pool works with any type, including tuples and custom objects
        var list = MTListPool<(int Id, string Name)>.Get();

        // Act
        list.Add((1, "Alice"));
        list.Add((2, "Bob"));

        // Assert
        Assert.Equal(2, list.Count);
        Assert.Equal((1, "Alice"), list[0]);

        // Cleanup
        MTListPool<(int, string)>.Release(list);
    }
}
