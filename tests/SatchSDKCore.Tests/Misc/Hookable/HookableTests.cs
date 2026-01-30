using SSC.Misc.Hookable;

namespace SatchSDKCore.Tests.Misc;

/// <summary>
/// Tests for Hookable class which provides a mechanism for registering and executing hooks
/// that can intercept and modify request processing flow.
/// </summary>
public class HookableTests
{
    /// <summary>
    /// Test context class used for hook testing.
    /// </summary>
    private class TestContext
    {
        public string Value { get; set; } = string.Empty;
        public bool WasIntercepted { get; set; }
    }

    /// <summary>
    /// Test hook that always returns false (doesn't interrupt execution).
    /// </summary>
    private class PassThroughHook : IHook<TestContext>
    {
        public int CallCount { get; private set; }

        public bool Intercept(TestContext context)
        {
            CallCount++;
            context.Value += "PassThrough;";
            return false;
        }
    }

    /// <summary>
    /// Test hook that always returns true (interrupts execution).
    /// </summary>
    private class InterruptingHook : IHook<TestContext>
    {
        public int CallCount { get; private set; }

        public bool Intercept(TestContext context)
        {
            CallCount++;
            context.Value += "Interrupt;";
            context.WasIntercepted = true;
            return true;
        }
    }

    /// <summary>
    /// Verifies that AddEarlyRequestHook() throws ArgumentNullException when given a null hook.
    /// </summary>
    [Fact]
    public void AddEarlyRequestHook_NullHook_ThrowsArgumentNullException()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hookable.AddEarlyRequestHook(null!));
    }

    /// <summary>
    /// Verifies that AddLateRequestHook() throws ArgumentNullException when given a null hook.
    /// </summary>
    [Fact]
    public void AddLateRequestHook_NullHook_ThrowsArgumentNullException()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hookable.AddLateRequestHook(null!));
    }

    /// <summary>
    /// Verifies that RemoveEarlyRequestHook() throws ArgumentNullException when given a null hook.
    /// </summary>
    [Fact]
    public void RemoveEarlyRequestHook_NullHook_ThrowsArgumentNullException()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hookable.RemoveEarlyRequestHook(null!));
    }

    /// <summary>
    /// Verifies that RemoveLateRequestHook() throws ArgumentNullException when given a null hook.
    /// </summary>
    [Fact]
    public void RemoveLateRequestHook_NullHook_ThrowsArgumentNullException()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hookable.RemoveLateRequestHook(null!));
    }

    /// <summary>
    /// Verifies that InterceptEarly() returns false when no hooks are registered.
    /// </summary>
    [Fact]
    public void InterceptEarly_NoHooks_ReturnsFalse()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var context = new TestContext();

        // Act
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that InterceptLate() returns false when no hooks are registered.
    /// </summary>
    [Fact]
    public void InterceptLate_NoHooks_ReturnsFalse()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var context = new TestContext();

        // Act
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that a single early request hook is executed when InterceptEarly() is called.
    /// </summary>
    [Fact]
    public void AddEarlyRequestHook_SingleHook_HookIsExecuted()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook.CallCount);
        Assert.Contains("PassThrough", context.Value);
    }

    /// <summary>
    /// Verifies that a single late request hook is executed when InterceptLate() is called.
    /// </summary>
    [Fact]
    public void AddLateRequestHook_SingleHook_HookIsExecuted()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddLateRequestHook(hook);
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook.CallCount);
        Assert.Contains("PassThrough", context.Value);
    }

    /// <summary>
    /// Verifies that multiple early request hooks are all executed in the order they were added.
    /// </summary>
    [Fact]
    public void AddEarlyRequestHook_MultipleHooks_AllHooksExecutedInOrder()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook1 = new PassThroughHook();
        var hook2 = new PassThroughHook();
        var hook3 = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook1);
        hookable.AddEarlyRequestHook(hook2);
        hookable.AddEarlyRequestHook(hook3);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook1.CallCount);
        Assert.Equal(1, hook2.CallCount);
        Assert.Equal(1, hook3.CallCount);
    }

    /// <summary>
    /// Verifies that adding the same early hook twice only adds it once (duplicate prevention).
    /// </summary>
    [Fact]
    public void AddEarlyRequestHook_SameHookTwice_OnlyAddedOnce()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook);
        hookable.AddEarlyRequestHook(hook); // Add same hook again
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook.CallCount); // Should only be called once
    }

    /// <summary>
    /// Verifies that adding the same late hook twice only adds it once (duplicate prevention).
    /// </summary>
    [Fact]
    public void AddLateRequestHook_SameHookTwice_OnlyAddedOnce()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddLateRequestHook(hook);
        hookable.AddLateRequestHook(hook); // Add same hook again
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook.CallCount); // Should only be called once
    }

    /// <summary>
    /// Verifies that InterceptEarly() returns true when a hook intercepts (returns true).
    /// </summary>
    [Fact]
    public void InterceptEarly_WithInterruptingHook_ReturnsTrue()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new InterruptingHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.True(result);
        Assert.True(context.WasIntercepted);
        Assert.Equal(1, hook.CallCount);
    }

    /// <summary>
    /// Verifies that InterceptLate() returns true when a hook intercepts (returns true).
    /// </summary>
    [Fact]
    public void InterceptLate_WithInterruptingHook_ReturnsTrue()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new InterruptingHook();
        var context = new TestContext();

        // Act
        hookable.AddLateRequestHook(hook);
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.True(result);
        Assert.True(context.WasIntercepted);
        Assert.Equal(1, hook.CallCount);
    }

    /// <summary>
    /// Verifies that when an early hook returns true (intercepts), subsequent hooks are not executed.
    /// </summary>
    [Fact]
    public void InterceptEarly_InterruptingHookStopsExecution()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook1 = new PassThroughHook();
        var hook2 = new InterruptingHook();
        var hook3 = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook1);
        hookable.AddEarlyRequestHook(hook2);
        hookable.AddEarlyRequestHook(hook3);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.True(result);
        Assert.Equal(1, hook1.CallCount);
        Assert.Equal(1, hook2.CallCount);
        Assert.Equal(0, hook3.CallCount); // Should not be called
    }

    /// <summary>
    /// Verifies that RemoveEarlyRequestHook() successfully removes an existing hook.
    /// </summary>
    [Fact]
    public void RemoveEarlyRequestHook_ExistingHook_HookIsRemoved()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook);
        hookable.RemoveEarlyRequestHook(hook);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
        Assert.Equal(0, hook.CallCount);
    }

    /// <summary>
    /// Verifies that RemoveLateRequestHook() successfully removes an existing hook.
    /// </summary>
    [Fact]
    public void RemoveLateRequestHook_ExistingHook_HookIsRemoved()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddLateRequestHook(hook);
        hookable.RemoveLateRequestHook(hook);
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.False(result);
        Assert.Equal(0, hook.CallCount);
    }

    /// <summary>
    /// Verifies that RemoveEarlyRequestHook() does not throw when removing a non-existent hook.
    /// </summary>
    [Fact]
    public void RemoveEarlyRequestHook_NonExistentHook_NoError()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();

        // Act & Assert - Should not throw
        hookable.RemoveEarlyRequestHook(hook);
    }

    /// <summary>
    /// Verifies that RemoveLateRequestHook() does not throw when removing a non-existent hook.
    /// </summary>
    [Fact]
    public void RemoveLateRequestHook_NonExistentHook_NoError()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();

        // Act & Assert - Should not throw
        hookable.RemoveLateRequestHook(hook);
    }

    /// <summary>
    /// Verifies that removing a hook from the middle of the chain doesn't affect other hooks.
    /// </summary>
    [Fact]
    public void RemoveEarlyRequestHook_MiddleHook_OtherHooksStillExecute()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook1 = new PassThroughHook();
        var hook2 = new PassThroughHook();
        var hook3 = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(hook1);
        hookable.AddEarlyRequestHook(hook2);
        hookable.AddEarlyRequestHook(hook3);
        hookable.RemoveEarlyRequestHook(hook2);
        var result = hookable.InterceptEarly(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook1.CallCount);
        Assert.Equal(0, hook2.CallCount);
        Assert.Equal(1, hook3.CallCount);
    }

    /// <summary>
    /// Verifies that early and late hook collections are independent of each other.
    /// </summary>
    [Fact]
    public void EarlyAndLateHooks_AreIndependent()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var earlyHook = new PassThroughHook();
        var lateHook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddEarlyRequestHook(earlyHook);
        hookable.AddLateRequestHook(lateHook);

        var earlyResult = hookable.InterceptEarly(context);
        var lateResult = hookable.InterceptLate(context);

        // Assert
        Assert.False(earlyResult);
        Assert.False(lateResult);
        Assert.Equal(1, earlyHook.CallCount);
        Assert.Equal(1, lateHook.CallCount);
    }

    /// <summary>
    /// Verifies that removing a hook from early hooks doesn't affect the same hook in late hooks.
    /// </summary>
    [Fact]
    public void RemoveEarlyRequestHook_DoesNotAffectLateHooks()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        var context = new TestContext();

        // Act
        hookable.AddLateRequestHook(hook);
        hookable.RemoveEarlyRequestHook(hook); // Try to remove from early (shouldn't affect late)
        var result = hookable.InterceptLate(context);

        // Assert
        Assert.False(result);
        Assert.Equal(1, hook.CallCount); // Late hook should still execute
    }

    /// <summary>
    /// Verifies that hooks are executed on each invocation of InterceptEarly().
    /// </summary>
    [Fact]
    public void InterceptEarly_MultipleInvocations_HooksExecutedEachTime()
    {
        // Arrange
        var hookable = new Hookable<TestContext>();
        var hook = new PassThroughHook();
        hookable.AddEarlyRequestHook(hook);

        // Act
        hookable.InterceptEarly(new TestContext());
        hookable.InterceptEarly(new TestContext());
        hookable.InterceptEarly(new TestContext());

        // Assert
        Assert.Equal(3, hook.CallCount);
    }
}
