using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for MTDictionaryPool class.
/// </summary>
public class MTDictionaryPoolTests
{
    [Fact]
    public void Get_ReturnsEmptyDictionary()
    {
        // Act
        // Get a Dictionary<string, int> from the pool - specialized pool for Dictionary type
        var dict = MTDictionaryPool<string, int>.Get();

        // Assert
        // Should return a ready-to-use empty dictionary
        Assert.NotNull(dict);
        Assert.Empty(dict);
        Assert.IsType<Dictionary<string, int>>(dict);
    }

    [Fact]
    public void Release_ClearsDictionary()
    {
        // Arrange
        // Get a dictionary and populate it with key-value pairs
        var dict = MTDictionaryPool<string, int>.Get();
        dict["key1"] = 1;
        dict["key2"] = 2;
        dict["key3"] = 3;

        // Act
        // Release clears all entries before returning to the pool
        MTDictionaryPool<string, int>.Release(dict);
        var newDict = MTDictionaryPool<string, int>.Get();

        // Assert
        // Next Get should return a clean, empty dictionary
        Assert.Empty(newDict);
    }

    [Fact]
    public void GetAndRelease_ReusesDictionaryInstance()
    {
        // Arrange
        var dict1 = MTDictionaryPool<int, string>.Get();
        dict1[1] = "one";
        dict1[2] = "two";

        // Act
        // Release the dictionary back to the pool, then get another
        MTDictionaryPool<int, string>.Release(dict1);
        var dict2 = MTDictionaryPool<int, string>.Get();

        // Assert
        // Pool should return the same instance to reduce allocations
        Assert.Same(dict1, dict2);
        Assert.Empty(dict2);
    }

    [Fact]
    public void DictionaryOperations_WorkNormally()
    {
        // Arrange
        // Pooled dictionaries should behave exactly like regular Dictionary<TKey, TValue>
        var dict = MTDictionaryPool<string, int>.Get();

        // Act & Assert
        // All standard Dictionary operations should work as expected
        dict.Add("one", 1);
        Assert.Single(dict);
        Assert.Equal(1, dict["one"]);

        dict["two"] = 2;
        dict["three"] = 3;
        Assert.Equal(3, dict.Count);

        Assert.True(dict.ContainsKey("two"));
        Assert.False(dict.ContainsKey("four"));

        dict.Remove("two");
        Assert.Equal(2, dict.Count);

        dict.Clear();
        Assert.Empty(dict);

        // Release back to pool when done
        MTDictionaryPool<string, int>.Release(dict);
    }

    [Fact]
    public void MultipleKeyValueTypes_CanBePooledIndependently()
    {
        // Act
        // Each generic type combination has its own independent pool
        var intStringDict = MTDictionaryPool<int, string>.Get();
        var stringIntDict = MTDictionaryPool<string, int>.Get();
        var stringStringDict = MTDictionaryPool<string, string>.Get();

        // Assert
        Assert.NotNull(intStringDict);
        Assert.NotNull(stringIntDict);
        Assert.NotNull(stringStringDict);

        // Verify they're different types - each pool manages its own TKey/TValue combo
        Assert.IsType<Dictionary<int, string>>(intStringDict);
        Assert.IsType<Dictionary<string, int>>(stringIntDict);
        Assert.IsType<Dictionary<string, string>>(stringStringDict);
    }

    [Fact]
    public void ComplexValueTypes_CanBePooled()
    {
        // Arrange
        // Pool works with any types, including tuples and custom objects as values
        var dict = MTDictionaryPool<int, (string Name, int Age)>.Get();

        // Act
        dict[1] = ("Alice", 30);
        dict[2] = ("Bob", 25);

        // Assert
        Assert.Equal(2, dict.Count);
        Assert.Equal(("Alice", 30), dict[1]);
        Assert.Equal(("Bob", 25), dict[2]);

        // Cleanup
        MTDictionaryPool<int, (string, int)>.Release(dict);
    }

    [Fact]
    public void TryGetValue_WorksCorrectly()
    {
        // Arrange
        // Verify that common Dictionary patterns work with pooled dictionaries
        var dict = MTDictionaryPool<string, int>.Get();
        dict["exists"] = 42;

        // Act & Assert
        // TryGetValue pattern should work as expected
        Assert.True(dict.TryGetValue("exists", out var value));
        Assert.Equal(42, value);

        Assert.False(dict.TryGetValue("notexists", out var _));

        // Cleanup
        MTDictionaryPool<string, int>.Release(dict);
    }
}
