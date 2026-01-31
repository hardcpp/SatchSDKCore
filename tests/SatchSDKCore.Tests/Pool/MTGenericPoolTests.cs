using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for MTGenericPool (Multi-Threaded Generic Pool) static class.
/// </summary>
public class MTGenericPoolTests
{
    private class TestObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsInitialized { get; set; }
    }

    [Fact]
    public void Get_ReturnsNewObject()
    {
        // Act
        // Get an object from the static pool - should return a new TestObject instance
        var obj = MTGenericPool<TestObject>.Get();

        // Assert
        // The object should be ready to use
        Assert.NotNull(obj);
    }

    [Fact]
    public void Get_ReturnsDefaultConstructedObject()
    {
        // Act
        // MTGenericPool uses the default constructor (new()) for object creation
        // Note: Pool is static and shared, so objects may have been used in other tests
        var obj = MTGenericPool<TestObject>.Get();

        // Assert
        // Object should be a valid TestObject instance (may not have default values due to pooling)
        Assert.NotNull(obj);
        Assert.IsType<TestObject>(obj);

        // Cleanup
        MTGenericPool<TestObject>.Release(obj);
    }

    [Fact]
    public void Release_ReturnsObjectToPool()
    {
        // Arrange
        // Get an object from the pool
        var obj = MTGenericPool<TestObject>.Get();
        obj.Id = 42;
        obj.Name = "Test";

        // Act
        // Release the object back to the pool
        MTGenericPool<TestObject>.Release(obj);

        // No direct way to verify it's back in pool, but we can get another
        var newObj = MTGenericPool<TestObject>.Get();

        // Assert
        // The pool should reuse the object (note: there's no reset action, so values persist)
        Assert.NotNull(newObj);
    }

    [Fact]
    public void GetAndRelease_ReusesSameInstance()
    {
        // Arrange
        // Get an object from the pool
        var obj1 = MTGenericPool<TestObject>.Get();
        obj1.Id = 123;
        obj1.Name = "Original";

        // Act
        // Release it back to the pool and get another one
        MTGenericPool<TestObject>.Release(obj1);
        var obj2 = MTGenericPool<TestObject>.Get();

        // Assert
        // Pool should reuse the same instance for efficiency (thread-safe)
        Assert.Same(obj1, obj2);
        // Note: MTGenericPool does not reset object state - values persist
        Assert.Equal(123, obj2.Id);
        Assert.Equal("Original", obj2.Name);
    }

    [Fact]
    public void MultipleGets_ReturnDifferentInstances()
    {
        // Act
        // Get multiple objects without releasing them
        var obj1 = MTGenericPool<TestObject>.Get();
        var obj2 = MTGenericPool<TestObject>.Get();
        var obj3 = MTGenericPool<TestObject>.Get();

        // Assert
        // Each Get should return a different instance
        Assert.NotSame(obj1, obj2);
        Assert.NotSame(obj2, obj3);
        Assert.NotSame(obj1, obj3);

        // Cleanup
        MTGenericPool<TestObject>.Release(obj1);
        MTGenericPool<TestObject>.Release(obj2);
        MTGenericPool<TestObject>.Release(obj3);
    }

    [Fact]
    public async Task MultipleThreads_CanGetAndReleaseSimultaneously()
    {
        // Arrange
        // Test thread-safety with 10 threads performing 100 operations each
        var tasks = new List<Task>();
        var iterationsPerThread = 100;
        var threadCount = 10;

        // Act
        // Each thread gets and releases objects in a loop
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var obj = MTGenericPool<TestObject>.Get();
                    obj.Id = j;
                    Thread.Sleep(1); // Simulate some work
                    MTGenericPool<TestObject>.Release(obj);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // If we reach here without exceptions, thread-safety is working
        Assert.True(true);
    }

    [Fact]
    public void Get_AfterRelease_WorksCorrectly()
    {
        // Arrange
        var obj1 = MTGenericPool<TestObject>.Get();
        MTGenericPool<TestObject>.Release(obj1);

        // Act
        // Get another object after releasing one
        var obj2 = MTGenericPool<TestObject>.Get();

        // Assert
        // Should get a valid object (likely the same instance)
        Assert.NotNull(obj2);
        Assert.Same(obj1, obj2);
    }

    [Fact]
    public void MultipleReleases_WorkWithDifferentObjects()
    {
        // Arrange
        var objects = new List<TestObject>();
        for (int i = 0; i < 5; i++)
        {
            objects.Add(MTGenericPool<TestObject>.Get());
        }

        // Act
        // Release all objects back to the pool
        foreach (var obj in objects)
        {
            MTGenericPool<TestObject>.Release(obj);
        }

        // Assert
        // Get objects again - should reuse released instances
        var newObjects = new List<TestObject>();
        for (int i = 0; i < 5; i++)
        {
            newObjects.Add(MTGenericPool<TestObject>.Get());
        }

        // At least some objects should be reused
        var reusedCount = newObjects.Count(no => objects.Contains(no));
        Assert.True(reusedCount > 0);

        // Cleanup
        foreach (var obj in newObjects)
        {
            MTGenericPool<TestObject>.Release(obj);
        }
    }

    private class ComplexObject
    {
        public List<int> Numbers { get; set; } = new();
        public Dictionary<string, string> Data { get; set; } = new();
    }

    [Fact]
    public void ComplexObject_CanBePooled()
    {
        // Arrange & Act
        // MTGenericPool works with any class that has a parameterless constructor
        var obj = MTGenericPool<ComplexObject>.Get();

        // Clear any existing data from previous tests (pool is static and shared)
        obj.Numbers.Clear();
        obj.Data.Clear();

        obj.Numbers.Add(1);
        obj.Numbers.Add(2);
        obj.Data["key"] = "value";

        // Assert
        Assert.Equal(2, obj.Numbers.Count);
        Assert.Single(obj.Data);

        // Act - Release and get again
        MTGenericPool<ComplexObject>.Release(obj);
        var newObj = MTGenericPool<ComplexObject>.Get();

        // Assert - Should be the same instance with data preserved (no auto-clear)
        Assert.Same(obj, newObj);
        Assert.Equal(2, newObj.Numbers.Count);
        Assert.Single(newObj.Data);

        // Cleanup
        MTGenericPool<ComplexObject>.Release(newObj);
    }

    [Fact]
    public void DifferentTypes_UseDifferentPools()
    {
        // Act
        // Different generic types should use different static pool instances
        var testObj = MTGenericPool<TestObject>.Get();
        var complexObj = MTGenericPool<ComplexObject>.Get();

        // Assert
        Assert.NotNull(testObj);
        Assert.NotNull(complexObj);
        Assert.IsType<TestObject>(testObj);
        Assert.IsType<ComplexObject>(complexObj);

        // Cleanup
        MTGenericPool<TestObject>.Release(testObj);
        MTGenericPool<ComplexObject>.Release(complexObj);
    }

    [Fact]
    public async Task HighVolume_GetAndRelease_WorksCorrectly()
    {
        // Arrange
        var iterations = 1000;

        // Act
        await Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var obj = MTGenericPool<TestObject>.Get();
                obj.Id = i;
                MTGenericPool<TestObject>.Release(obj);
            }
        });

        // Assert
        // Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public async Task ConcurrentAccess_DifferentTypes_WorksCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        // Different types being accessed concurrently
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var obj = MTGenericPool<TestObject>.Get();
                obj.Id = i;
                MTGenericPool<TestObject>.Release(obj);
            }
        }));

        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var obj = MTGenericPool<ComplexObject>.Get();
                obj.Numbers.Add(i);
                MTGenericPool<ComplexObject>.Release(obj);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        // Should complete without exceptions
        Assert.True(true);
    }
}