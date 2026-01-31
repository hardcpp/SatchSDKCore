using SSC.Pool;

namespace SSC.Tests.Pool;

/// <summary>
/// Tests for MTObjectPool (Multi-Threaded Object Pool) class.
/// </summary>
public class MTObjectPoolTests : IDisposable
{
    private readonly List<MTObjectPool<TestObject>> _poolsToDispose = new();

    private class TestObject
    {
        public int Id { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsReleased { get; set; }
        public bool IsDestroyed { get; set; }
    }

    private MTObjectPool<TestObject> CreatePool(
        Func<TestObject>? createFunc = null,
        Action<TestObject>? actionOnGet = null,
        Action<TestObject>? actionOnRelease = null,
        Action<TestObject>? actionOnDestroy = null,
        bool collectionCheck = true,
        int defaultCapacity = 10,
        int maxSize = 100)
    {
        var pool = new MTObjectPool<TestObject>(
            createFunc ?? (() => new TestObject()),
            actionOnGet,
            actionOnRelease,
            actionOnDestroy,
            collectionCheck,
            defaultCapacity,
            maxSize);
        _poolsToDispose.Add(pool);
        return pool;
    }

    public void Dispose()
    {
        foreach (var pool in _poolsToDispose)
        {
            pool.Dispose();
        }
        _poolsToDispose.Clear();
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesPool()
    {
        // Act
        // Create a thread-safe pool with 5 pre-allocated objects
        var pool = CreatePool(defaultCapacity: 5);

        // Assert
        // MTObjectPool should be created with all objects ready and inactive
        Assert.NotNull(pool);
        Assert.Equal(5, pool.CountAll);
        Assert.Equal(5, pool.CountInactive);
        Assert.Equal(0, pool.CountActive);
    }

    [Fact]
    public void Constructor_WithNullCreateFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        // Factory function is required - cannot be null
        Assert.Throws<ArgumentNullException>(() => new MTObjectPool<TestObject>(null!));
    }

    [Fact]
    public void Constructor_WithZeroMaxSize_ThrowsArgumentException()
    {
        // Act & Assert
        // Max size must be positive
        Assert.Throws<ArgumentException>(() => CreatePool(maxSize: 0));
    }

    [Fact]
    public void Constructor_WithNegativeMaxSize_ThrowsArgumentException()
    {
        // Act & Assert
        // Max size cannot be negative
        Assert.Throws<ArgumentException>(() => CreatePool(maxSize: -1));
    }

    [Fact]
    public void Get_FromEmptyPool_CreatesNewObject()
    {
        // Arrange
        // Start with an empty pool (no pre-allocated objects)
        var pool = CreatePool(defaultCapacity: 0);

        // Act
        // Getting from an empty pool creates a new object on demand
        var obj = pool.Get();

        // Assert
        // Pool now has 1 object which is active
        Assert.NotNull(obj);
        Assert.Equal(1, pool.CountAll);
        Assert.Equal(1, pool.CountActive);
        Assert.Equal(0, pool.CountInactive);
    }

    [Fact]
    public void Get_FromPopulatedPool_ReturnsExistingObject()
    {
        // Arrange
        // Pool with 5 pre-allocated objects
        var pool = CreatePool(defaultCapacity: 5);
        var initialCount = pool.CountAll;

        // Act
        // Getting from a populated pool reuses an existing object
        var obj = pool.Get();

        // Assert
        // No new objects created, one moved from inactive to active
        Assert.NotNull(obj);
        Assert.Equal(initialCount, pool.CountAll);
        Assert.Equal(1, pool.CountActive);
        Assert.Equal(4, pool.CountInactive);
    }

    [Fact]
    public void Get_CallsActionOnGet()
    {
        // Arrange
        // actionOnGet callback is invoked when an object is retrieved
        var actionCalled = false;
        TestObject? actionObject = null;
        var pool = CreatePool(
            actionOnGet: obj =>
            {
                actionCalled = true;
                actionObject = obj;
            });

        // Act
        var obj = pool.Get();

        // Assert
        // Callback should have been invoked with the retrieved object
        Assert.True(actionCalled);
        Assert.Same(obj, actionObject);
    }

    [Fact]
    public void Release_ValidObject_ReturnsToPool()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);
        var obj = pool.Get();

        // Act
        // Release returns the object to the pool for reuse (thread-safe)
        pool.Release(obj);

        // Assert
        // Object moved from active back to inactive
        Assert.Equal(5, pool.CountInactive);
        Assert.Equal(0, pool.CountActive);
    }

    [Fact]
    public void Release_CallsActionOnRelease()
    {
        // Arrange
        // actionOnRelease callback is invoked when an object is returned
        var actionCalled = false;
        TestObject? actionObject = null;
        var pool = CreatePool(
            actionOnRelease: obj =>
            {
                actionCalled = true;
                actionObject = obj;
            });
        var obj = pool.Get();

        // Act
        pool.Release(obj);

        // Assert
        // Callback should have been invoked with the released object
        Assert.True(actionCalled);
        Assert.Same(obj, actionObject);
    }

    [Fact]
    public void Release_SameObjectTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        // With collectionCheck enabled, double-release is detected (thread-safe)
        var pool = CreatePool(collectionCheck: true);
        var obj = pool.Get();
        pool.Release(obj);

        // Act & Assert
        // Attempting to release the same object twice should throw
        Assert.Throws<InvalidOperationException>(() => pool.Release(obj));
    }

    [Fact]
    public void Release_SameObjectTwiceWithoutCollectionCheck_DoesNotThrow()
    {
        // Arrange
        // Without collectionCheck, double-release is allowed (but adds duplicate)
        var pool = CreatePool(collectionCheck: false, defaultCapacity: 0);
        var obj = pool.Get();
        pool.Release(obj);

        // Act & Assert - Should not throw but creates duplicate in pool
        pool.Release(obj);
        Assert.Equal(2, pool.CountInactive);
    }

    [Fact]
    public void Release_WhenPoolFull_CallsActionOnDestroy()
    {
        // Arrange
        // When pool reaches maxSize, additional releases trigger destruction
        var destroyCalled = false;
        TestObject? destroyedObject = null;
        var pool = CreatePool(
            defaultCapacity: 0,
            maxSize: 2,
            actionOnDestroy: obj =>
            {
                destroyCalled = true;
                destroyedObject = obj;
            });

        var obj1 = pool.Get();
        var obj2 = pool.Get();
        var obj3 = pool.Get();

        pool.Release(obj1);
        pool.Release(obj2);  // Pool now at maxSize (2)

        // Act - This release exceeds maxSize, so object is destroyed instead
        pool.Release(obj3);

        // Assert
        // obj3 should be destroyed, not added to pool
        Assert.True(destroyCalled);
        Assert.Same(obj3, destroyedObject);
        Assert.Equal(2, pool.CountInactive);
    }

    [Fact]
    public void GetWithOutParameter_ReturnsPooledObjectAndElement()
    {
        // Arrange
        var pool = CreatePool();

        // Act
        // Get method can return both a PooledObject wrapper and the element
        var pooledObj = pool.Get(out var element);

        // Assert
        // Both references point to the same object
        Assert.NotNull(pooledObj);
        Assert.NotNull(element);
        Assert.Same(element, pooledObj.Value);
        Assert.Equal(1, pool.CountActive);
    }

    [Fact]
    public void PooledObject_Dispose_ReleasesBackToPool()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);
        var pooledObj = pool.Get(out var element);
        Assert.Equal(1, pool.CountActive);

        // Act
        // Disposing the PooledObject wrapper automatically releases it (thread-safe)
        (pooledObj as IDisposable).Dispose();

        // Assert
        // Object should be back in the pool
        Assert.Equal(0, pool.CountActive);
        Assert.Equal(5, pool.CountInactive);
    }

    [Fact]
    public void Clear_EmptiesPool()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);
        var obj1 = pool.Get();
        var obj2 = pool.Get();

        // Act
        // Clear removes all objects from the pool (thread-safe)
        pool.Clear();

        // Assert
        // Pool should be completely empty
        Assert.Equal(0, pool.CountAll);
        Assert.Equal(0, pool.CountInactive);
        Assert.Equal(0, pool.CountActive);
    }

    [Fact]
    public void Clear_CallsActionOnDestroyForAllObjects()
    {
        // Arrange
        // actionOnDestroy is called for each object when clearing
        var destroyCount = 0;
        var pool = CreatePool(
            defaultCapacity: 5,
            actionOnDestroy: obj => destroyCount++);

        // Act
        pool.Clear();

        // Assert
        // All 5 objects should be destroyed
        Assert.Equal(5, destroyCount);
    }

    [Fact]
    public void Dispose_CallsClear()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);

        // Act
        // Dispose cleans up all pool resources
        pool.Dispose();

        // Assert
        // Pool should be empty after disposal
        Assert.Equal(0, pool.CountAll);
        Assert.Equal(0, pool.CountInactive);
    }

    [Fact]
    public async Task MultipleThreads_CanGetAndReleaseSimultaneously()
    {
        // Arrange
        // Test thread-safety with 10 threads performing 100 operations each
        var pool = CreatePool(defaultCapacity: 10, maxSize: 100);
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
                    var obj = pool.Get();
                    Thread.Sleep(1); // Simulate some work
                    pool.Release(obj);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // All objects should be released back to the pool
        Assert.Equal(0, pool.CountActive);
        Assert.True(pool.CountInactive <= 100); // Should not exceed maxSize
    }

    [Fact]
    public void CountProperties_ReflectCorrectState()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);

        // Initial state
        Assert.Equal(5, pool.CountAll);
        Assert.Equal(5, pool.CountInactive);
        Assert.Equal(0, pool.CountActive);

        // Get 3 objects
        var obj1 = pool.Get();
        var obj2 = pool.Get();
        var obj3 = pool.Get();

        Assert.Equal(5, pool.CountAll);
        Assert.Equal(2, pool.CountInactive);
        Assert.Equal(3, pool.CountActive);

        // Release 1 object
        pool.Release(obj1);

        Assert.Equal(5, pool.CountAll);
        Assert.Equal(3, pool.CountInactive);
        Assert.Equal(2, pool.CountActive);

        // Get 1 more object (creates new since we started with 5 and got 3)
        var obj4 = pool.Get();

        Assert.Equal(5, pool.CountAll);
        Assert.Equal(2, pool.CountInactive);
        Assert.Equal(3, pool.CountActive);
    }

    [Fact]
    public void ActionOnGet_SetObjectState()
    {
        // Arrange
        var pool = CreatePool(
            actionOnGet: obj => obj.IsInitialized = true);

        // Act
        var obj = pool.Get();

        // Assert
        Assert.True(obj.IsInitialized);
    }

    [Fact]
    public void ActionOnRelease_ResetObjectState()
    {
        // Arrange
        var pool = CreatePool(
            actionOnRelease: obj =>
            {
                obj.IsReleased = true;
                obj.IsInitialized = false;
            });
        var obj = pool.Get();
        obj.IsInitialized = true;

        // Act
        pool.Release(obj);

        // Assert
        Assert.True(obj.IsReleased);
        Assert.False(obj.IsInitialized);
    }

    [Fact]
    public void Pool_CanBeReusedAfterClear()
    {
        // Arrange
        var pool = CreatePool(defaultCapacity: 5);
        pool.Clear();

        // Act
        var obj = pool.Get();

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, pool.CountAll);
        Assert.Equal(1, pool.CountActive);
    }
}
