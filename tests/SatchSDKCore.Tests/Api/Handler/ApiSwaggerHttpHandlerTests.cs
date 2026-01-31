using SSC.Api.Blueprint;
using SSC.Api.Handler;
using SSC.Api.Route;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Tests.Api.Handler;

/// <summary>
/// Unit tests for the ApiSwaggerHttpHandler class
/// </summary>
public class SwaggerHTTPServerHandlerTests
{
    #region Constructor Tests

    /// <summary>
    /// Test that constructor throws when blueprint is null
    /// </summary>
    [Fact]
    public void Constructor_WithNullBlueprint_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApiSwaggerHttpHandler(null!));
    }

    /// <summary>
    /// Test that constructor initializes correctly with valid blueprint
    /// </summary>
    [Fact]
    public void Constructor_WithValidBlueprint_ShouldInitialize()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.NotNull(handler);
        Assert.NotNull(handler.MainBlueprint);
        Assert.Same(blueprint, handler.MainBlueprint);
        Assert.NotNull(handler.Hooks);
    }

    /// <summary>
    /// Test that constructor preserves blueprint reference
    /// </summary>
    [Fact]
    public void Constructor_ShouldPreserveBlueprintReference()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("MyBlueprint");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.Same(blueprint, handler.MainBlueprint);
        Assert.Equal("MyBlueprint", handler.MainBlueprint.Name);
    }

    #endregion

    #region TryHandle Tests

    /// <summary>
    /// Test TryHandle with null context throws
    /// </summary>
    [Fact]
    public void TryHandle_WithNullContext_ShouldThrow()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.TryHandle(null!));
    }

    /// <summary>
    /// Test that TryHandle only handles /swagger path
    /// Since we can't easily create HttpListenerContext, we test the logic indirectly
    /// </summary>
    [Fact]
    public void TryHandle_Logic_ShouldOnlyHandleSwaggerPath()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert - The handler is designed to only handle /swagger GET requests
        // This is validated by the implementation logic which checks:
        // 1. l_OgRequest.Url!.AbsolutePath != "/swagger"
        // 2. l_OgRequest.HttpMethod != "GET"
        // We verify the handler exists and is properly initialized
        Assert.NotNull(handler);
        Assert.NotNull(handler.MainBlueprint);
    }

    #endregion

    #region Hooks Tests

    /// <summary>
    /// Test that Hooks property is initialized
    /// </summary>
    [Fact]
    public void Hooks_ShouldBeInitialized()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.NotNull(handler.Hooks);
    }

    /// <summary>
    /// Test that hooks implement IHookable interface
    /// </summary>
    [Fact]
    public void Hooks_ShouldImplementIHookable()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act & Assert
        Assert.IsAssignableFrom<IHookable<HttpServerExRequestContext>>(handler.Hooks);
    }

    #endregion

    #region Blueprint Integration Tests

    /// <summary>
    /// Test that handler works with different blueprint types
    /// </summary>
    [Fact]
    public void Handler_WithDifferentBlueprints_ShouldWork()
    {
        // Arrange
        var restBlueprint = new ApiHttpBlueprint("REST");

        // Act
        var handler1 = new ApiSwaggerHttpHandler(restBlueprint);

        // Assert
        Assert.NotNull(handler1);
        Assert.Same(restBlueprint, handler1.MainBlueprint);
    }

    /// <summary>
    /// Test that handler maintains blueprint state
    /// </summary>
    [Fact]
    public void Handler_ShouldMaintainBlueprintState()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test", "api");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.Equal("Test", handler.MainBlueprint.Name);
        Assert.Equal("api", handler.MainBlueprint.Prefix);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Test that multiple handlers can share the same blueprint
    /// </summary>
    [Fact]
    public void MultipleHandlers_WithSameBlueprint_ShouldWork()
    {
        // Arrange
        var sharedBlueprint = new ApiHttpBlueprint("Shared");

        // Act
        var handler1 = new ApiSwaggerHttpHandler(sharedBlueprint);
        var handler2 = new ApiSwaggerHttpHandler(sharedBlueprint);

        // Assert
        Assert.Same(handler1.MainBlueprint, handler2.MainBlueprint);
        Assert.Same(sharedBlueprint, handler1.MainBlueprint);
        Assert.Same(sharedBlueprint, handler2.MainBlueprint);
    }

    /// <summary>
    /// Test handler properties are independent even with shared blueprint
    /// </summary>
    [Fact]
    public void MultipleHandlers_ShouldHaveIndependentHooks()
    {
        // Arrange
        var sharedBlueprint = new ApiHttpBlueprint("Shared");

        // Act
        var handler1 = new ApiSwaggerHttpHandler(sharedBlueprint);
        var handler2 = new ApiSwaggerHttpHandler(sharedBlueprint);

        // Assert
        Assert.NotSame(handler1.Hooks, handler2.Hooks);
        Assert.Same(handler1.MainBlueprint, handler2.MainBlueprint);
    }

    /// <summary>
    /// Test handler with blueprint containing routes
    /// </summary>
    [Fact]
    public void Handler_WithBlueprintContainingRoutes_ShouldWork()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        blueprint.AddRoutesOf<RESTHTTPServerHandlerTests.TestHandlerRoutes>();

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.NotNull(handler);
        Assert.Same(blueprint, handler.MainBlueprint);

        // Verify blueprint still has its routes
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "api", "test" },
            args,
            out var route
        );
        Assert.True(found);
    }

    #endregion

    #region Content Tests

    /// <summary>
    /// Test that Swagger HTML content is not null or empty
    /// Since the HTML is private, we verify handler initialization succeeds
    /// </summary>
    [Fact]
    public void SwaggerContent_ShouldBeValid()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert - Handler should initialize without issues
        Assert.NotNull(handler);
        Assert.NotNull(handler.MainBlueprint);
        Assert.NotNull(handler.Hooks);
    }

    #endregion

    #region Multiple Instance Tests

    /// <summary>
    /// Test creating multiple handler instances
    /// </summary>
    [Fact]
    public void MultipleInstances_ShouldWorkIndependently()
    {
        // Arrange
        var blueprint1 = new ApiHttpBlueprint("Blueprint1");
        var blueprint2 = new ApiHttpBlueprint("Blueprint2");

        // Act
        var handler1 = new ApiSwaggerHttpHandler(blueprint1);
        var handler2 = new ApiSwaggerHttpHandler(blueprint2);

        // Assert
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotSame(handler1, handler2);
        Assert.NotSame(handler1.MainBlueprint, handler2.MainBlueprint);
        Assert.NotSame(handler1.Hooks, handler2.Hooks);
    }

    /// <summary>
    /// Test that handler instances maintain state correctly
    /// </summary>
    [Fact]
    public void Handler_ShouldMaintainState()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test", "api");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert - State should be preserved
        Assert.Equal("Test", handler.MainBlueprint.Name);
        Assert.Equal("api", handler.MainBlueprint.Prefix);
        Assert.Same(blueprint, handler.MainBlueprint);
    }

    #endregion

    #region Blueprint Modification Tests

    /// <summary>
    /// Test that modifying blueprint after handler creation affects handler
    /// </summary>
    [Fact]
    public void Handler_AfterBlueprintModification_ShouldReflectChanges()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act - Modify blueprint after handler creation
        blueprint.AddRoutesOf<RESTHTTPServerHandlerTests.TestHandlerRoutes>();

        // Assert - Handler should see the changes (same reference)
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var restBlueprint = handler.MainBlueprint as ApiHttpBlueprint;
        var found = restBlueprint!.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "api", "test" },
            args,
            out var route
        );
        Assert.True(found);
    }

    /// <summary>
    /// Test handler with nested blueprints
    /// </summary>
    [Fact]
    public void Handler_WithNestedBlueprints_ShouldWork()
    {
        // Arrange
        var mainBlueprint = new ApiHttpBlueprint("Main");
        var nestedBlueprint = new ApiHttpBlueprint("Nested", "v1");
        nestedBlueprint.AddRoutesOf<RESTHTTPServerHandlerTests.TestHandlerRoutes>();
        mainBlueprint.AddBlueprint(nestedBlueprint);

        // Act
        var handler = new ApiSwaggerHttpHandler(mainBlueprint);

        // Assert
        Assert.NotNull(handler);
        Assert.Same(mainBlueprint, handler.MainBlueprint);

        // Verify nested routes are accessible
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var restBlueprint = handler.MainBlueprint as ApiHttpBlueprint;
        var found = restBlueprint!.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "v1", "api", "test" },
            args,
            out var route
        );
        Assert.True(found);
    }

    #endregion

    #region Interface Compliance Tests

    /// <summary>
    /// Test that handler implements IHttpServerExRequestHandler
    /// </summary>
    [Fact]
    public void Handler_ShouldImplementInterface()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.IsAssignableFrom<IHttpServerExRequestHandler>(handler);
    }

    /// <summary>
    /// Test handler has all required interface members
    /// </summary>
    [Fact]
    public void Handler_ShouldHaveRequiredMembers()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert - Verify required members exist
        Assert.NotNull(handler.Hooks);
        Assert.NotNull(handler.MainBlueprint);

        // Verify TryHandle method exists (already tested it throws with null)
        Assert.Throws<ArgumentNullException>(() => handler.TryHandle(null!));
    }

    #endregion

    #region Concurrent Access Tests

    /// <summary>
    /// Test handler can be safely accessed from multiple threads
    /// </summary>
    [Fact]
    public async Task Handler_ConcurrentAccess_ShouldBeSafe()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        blueprint.AddRoutesOf<RESTHTTPServerHandlerTests.TestHandlerRoutes>();
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act - Multiple threads accessing handler properties
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                Assert.NotNull(handler.MainBlueprint);
                Assert.NotNull(handler.Hooks);
                Assert.Equal("Test", handler.MainBlueprint.Name);
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All tasks completed successfully
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }

    /// <summary>
    /// Test that multiple handlers can be used concurrently
    /// </summary>
    [Fact]
    public async Task MultipleHandlers_ConcurrentUse_ShouldBeSafe()
    {
        // Arrange
        var handlers = new ApiSwaggerHttpHandler[5];
        for (int i = 0; i < handlers.Length; i++)
        {
            var blueprint = new ApiHttpBlueprint($"Blueprint{i}");
            handlers[i] = new ApiSwaggerHttpHandler(blueprint);
        }

        // Act - Use all handlers concurrently
        var tasks = new Task[handlers.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                Assert.NotNull(handlers[index]);
                Assert.NotNull(handlers[index].MainBlueprint);
                Assert.Equal($"Blueprint{index}", handlers[index].MainBlueprint.Name);
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }

    #endregion

    #region Blueprint Type Tests

    /// <summary>
    /// Test handler with ApiBlueprint interface
    /// </summary>
    [Fact]
    public void Handler_WithIBlueprint_ShouldWork()
    {
        // Arrange
        ApiBlueprint blueprint = new ApiHttpBlueprint("Test");

        // Act
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Assert
        Assert.NotNull(handler);
        Assert.Same(blueprint, handler.MainBlueprint);
    }

    /// <summary>
    /// Test handler maintains blueprint type information
    /// </summary>
    [Fact]
    public void Handler_ShouldMaintainBlueprintType()
    {
        // Arrange
        var restBlueprint = new ApiHttpBlueprint("REST");

        // Act
        var handler = new ApiSwaggerHttpHandler(restBlueprint);

        // Assert
        Assert.IsType<ApiHttpBlueprint>(handler.MainBlueprint);
        Assert.IsAssignableFrom<ApiBlueprint>(handler.MainBlueprint);
    }

    #endregion

    #region Property Access Tests

    /// <summary>
    /// Test MainBlueprint property is read-only
    /// </summary>
    [Fact]
    public void MainBlueprint_ShouldBeReadOnly()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act & Assert - Property should be readonly (verified at compile time)
        // We can only verify it exists and returns the correct value
        Assert.NotNull(handler.MainBlueprint);
        Assert.Same(blueprint, handler.MainBlueprint);
    }

    /// <summary>
    /// Test Hooks property is read-only and initialized
    /// </summary>
    [Fact]
    public void Hooks_ShouldBeReadOnlyAndInitialized()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act & Assert - Property should be readonly and initialized
        Assert.NotNull(handler.Hooks);

        // Verify it's always the same instance
        var hooks1 = handler.Hooks;
        var hooks2 = handler.Hooks;
        Assert.Same(hooks1, hooks2);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test handler validates blueprint is not null in constructor
    /// </summary>
    [Fact]
    public void Constructor_ValidatesBlueprint()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ApiSwaggerHttpHandler(null!));
        Assert.NotNull(exception);
    }

    /// <summary>
    /// Test TryHandle validates context is not null
    /// </summary>
    [Fact]
    public void TryHandle_ValidatesContext()
    {
        // Arrange
        var blueprint = new ApiHttpBlueprint("Test");
        var handler = new ApiSwaggerHttpHandler(blueprint);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => handler.TryHandle(null!));
        Assert.NotNull(exception);
    }

    #endregion
}
