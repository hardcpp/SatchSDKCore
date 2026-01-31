using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for PooledObject class.
/// </summary>
public class PooledObjectTests
{
    private class TestObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void Value_WhenNotDisposed_ReturnsObject()
    {
        // Arrange
        // Create a pool with one pre-allocated object
        var pool = new MTObjectPool<TestObject>(() => new TestObject { Id = 1 }, defaultCapacity: 1);
        var pooledObj = pool.Get(out var element);

        // Act
        // Access the Value property which should return the pooled object
        var value = pooledObj.Value;

        // Assert
        // The Value should be the same object we got from the pool
        Assert.NotNull(value);
        Assert.Same(element, value);
        Assert.Equal(1, value.Id);
    }

    [Fact]
    public void Value_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var pool = new MTObjectPool<TestObject>(() => new TestObject(), defaultCapacity: 1);
        var pooledObj = pool.Get(out var _);

        // Act
        // Dispose the pooled object, which returns it to the pool
        (pooledObj as IDisposable).Dispose();

        // Assert
        // Attempting to access Value after disposal should throw
        Assert.Throws<ObjectDisposedException>(() => pooledObj.Value);
    }

    [Fact]
    public void Dispose_ReleasesObjectBackToPool()
    {
        // Arrange
        // Get an object from the pool, which reduces inactive count by 1
        var pool = new MTObjectPool<TestObject>(() => new TestObject(), defaultCapacity: 1);
        var initialInactive = pool.CountInactive;
        var pooledObj = pool.Get(out var _);
        Assert.Equal(initialInactive - 1, pool.CountInactive);

        // Act
        // Dispose should return the object to the pool
        (pooledObj as IDisposable).Dispose();

        // Assert
        // The inactive count should be restored to the initial value
        Assert.Equal(initialInactive, pool.CountInactive);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_OnlyReleasesOnce()
    {
        // Arrange
        // Track how many times the release action is called
        var releaseCount = 0;
        var pool = new MTObjectPool<TestObject>(
            () => new TestObject(),
            actionOnRelease: _ => releaseCount++,
            defaultCapacity: 1);
        var pooledObj = pool.Get(out var _);

        // Act
        // Call Dispose multiple times - this is a common pattern to ensure idempotence
        (pooledObj as IDisposable).Dispose();
        (pooledObj as IDisposable).Dispose();
        (pooledObj as IDisposable).Dispose();

        // Assert
        // The object should only be released once, preventing double-release bugs
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public void UsingStatement_AutomaticallyDisposesAndReturnsToPool()
    {
        // Arrange
        var pool = new MTObjectPool<TestObject>(() => new TestObject(), defaultCapacity: 1);
        var initialInactive = pool.CountInactive;

        // Act
        // Using statement ensures Dispose is called when scope exits
        using (var pooledObj = pool.Get(out var element))
        {
            // Inside the using block, the object is active
            Assert.NotNull(pooledObj.Value);
            Assert.Same(element, pooledObj.Value);
            Assert.Equal(initialInactive - 1, pool.CountInactive);
        }
        // When the using block exits, Dispose is automatically called

        // Assert - Object should be returned to pool after using block
        Assert.Equal(initialInactive, pool.CountInactive);
    }

    [Fact]
    public void MultiplePooledObjects_CanExistSimultaneously()
    {
        // Arrange
        var pool = new MTObjectPool<TestObject>(() => new TestObject(), defaultCapacity: 5);

        // Act
        // Get multiple objects from the pool at the same time
        var pooled1 = pool.Get(out var obj1);
        var pooled2 = pool.Get(out var obj2);
        var pooled3 = pool.Get(out var obj3);

        // Assert
        // Each object should be a distinct instance
        Assert.NotSame(obj1, obj2);
        Assert.NotSame(obj2, obj3);
        Assert.NotSame(obj1, obj3);

        // Each PooledObject wrapper should contain its corresponding object
        Assert.Same(obj1, pooled1.Value);
        Assert.Same(obj2, pooled2.Value);
        Assert.Same(obj3, pooled3.Value);

        // Pool should track all 3 as active
        Assert.Equal(3, pool.CountActive);
    }

    [Fact]
    public void PooledObject_WorksWithComplexTypes()
    {
        // Arrange
        // Create a pool with an actionOnGet callback that modifies the object state
        var pool = new MTObjectPool<TestObject>(
            () => new TestObject { Id = 0, Name = "Default" },
            actionOnGet: obj =>
            {
                // Initialize the object when retrieved from the pool
                obj.Id++;
                obj.Name = $"Object {obj.Id}";
            },
            defaultCapacity: 1);

        // Act
        var pooled = pool.Get(out var obj);

        // Assert
        // The actionOnGet should have been called to initialize the object
        Assert.Equal(1, obj.Id);
        Assert.Equal("Object 1", obj.Name);
        Assert.Same(obj, pooled.Value);
    }

    [Fact]
    public void NestedPooledObjects_CanBeUsedTogether()
    {
        // Arrange
        var pool = new MTObjectPool<TestObject>(() => new TestObject(), defaultCapacity: 5);

        // Act & Assert
        // Test nested using statements - common pattern for scoped resource management
        using (var outer = pool.Get(out var outerObj))
        {
            outerObj.Id = 1;

            using (var inner = pool.Get(out var innerObj))
            {
                innerObj.Id = 2;

                // Both objects should be active and different
                Assert.NotSame(outerObj, innerObj);
                Assert.Equal(1, outerObj.Id);
                Assert.Equal(2, innerObj.Id);
                Assert.Equal(2, pool.CountActive);
            }
            // Inner using block ends, inner object is released

            // Inner should be released, outer still active
            Assert.Equal(1, pool.CountActive);
        }
        // Outer using block ends, outer object is released

        // Both should be released back to the pool
        Assert.Equal(0, pool.CountActive);
    }

    [Fact]
    public void PooledObject_WithSTObjectPool_WorksCorrectly()
    {
        // Arrange
        // PooledObject should work with single-threaded pools too
        var pool = new STObjectPool<TestObject>(() => new TestObject { Id = 42 }, defaultCapacity: 1);

        // Act
        using (var pooled = pool.Get(out var obj))
        {
            // Assert
            // PooledObject wrapper works the same regardless of pool type
            Assert.NotNull(pooled.Value);
            Assert.Same(obj, pooled.Value);
            Assert.Equal(42, obj.Id);
        }

        // Verify returned to pool after using block
        Assert.Equal(0, pool.CountActive);
    }
}