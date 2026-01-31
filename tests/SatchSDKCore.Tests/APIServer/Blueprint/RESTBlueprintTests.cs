using System;
using System.Collections.Generic;
using Xunit;
using SSC.APIServer.Blueprint;
using SSC.APIServer.Route;
using SSC.APIServer.Response;
using SSC.APIServer.RouteContext;

namespace SSC.Tests.APIServer.Blueprint;

/// <summary>
/// Unit tests for the RESTBlueprint class
/// </summary>
public class RESTBlueprintTests
{
    #region Test Helper Classes

    /// <summary>
    /// Test route class for testing route registration
    /// </summary>
    public class TestRoutes
    {
        [RESTRoute(ERestMethod.Get, "/test")]
        public static IResponse GetTest(RESTRouteContext context)
        {
            return RESTResponse.Result(context, System.Net.HttpStatusCode.OK, "Test response");
        }

        [RESTRoute(ERestMethod.Post, "/users")]
        public static IResponse PostUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, System.Net.HttpStatusCode.Created, "User created");
        }

        [RESTRoute(ERestMethod.Get, "/users/<id>")]
        public static IResponse GetUserById(RESTRouteContext context)
        {
            return RESTResponse.Result(context, System.Net.HttpStatusCode.OK, "User found");
        }

        [RESTRoute(ERestMethod.Delete, "/users/<id>")]
        public static IResponse DeleteUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, System.Net.HttpStatusCode.NoContent, "");
        }

        [RESTRoute(ERestMethod.Put, "/users/<id>/settings/<settingId>")]
        public static IResponse UpdateUserSetting(RESTRouteContext context)
        {
            return RESTResponse.Result(context, System.Net.HttpStatusCode.OK, "Setting updated");
        }
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Test that constructor initializes with valid name
    /// </summary>
    [Fact]
    public void Constructor_WithValidName_ShouldInitialize()
    {
        // Arrange & Act
        var blueprint = new RESTBlueprint("test-blueprint");

        // Assert
        Assert.NotNull(blueprint);
        Assert.Equal("test-blueprint", blueprint.Name);
        Assert.Null(blueprint.Prefix);
    }

    /// <summary>
    /// Test that constructor initializes with name and prefix
    /// </summary>
    [Fact]
    public void Constructor_WithValidNameAndPrefix_ShouldInitialize()
    {
        // Arrange & Act
        var blueprint = new RESTBlueprint("test-blueprint", "api/v1");

        // Assert
        Assert.NotNull(blueprint);
        Assert.Equal("test-blueprint", blueprint.Name);
        Assert.Equal("api/v1", blueprint.Prefix);
    }

    /// <summary>
    /// Test that constructor throws when name is null
    /// </summary>
    [Fact]
    public void Constructor_WithNullName_ShouldThrow()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RESTBlueprint(null!));
    }

    /// <summary>
    /// Test that constructor throws when prefix starts with slash
    /// </summary>
    [Fact]
    public void Constructor_WithPrefixStartingWithSlash_ShouldThrow()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<Exception>(() => new RESTBlueprint("test", "/api"));
        Assert.Contains("can not start or end by '/'", ex.Message);
    }

    /// <summary>
    /// Test that constructor throws when prefix ends with slash
    /// </summary>
    [Fact]
    public void Constructor_WithPrefixEndingWithSlash_ShouldThrow()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<Exception>(() => new RESTBlueprint("test", "api/"));
        Assert.Contains("can not start or end by '/'", ex.Message);
    }

    /// <summary>
    /// Test that constructor allows empty prefix
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyPrefix_ShouldInitialize()
    {
        // Arrange & Act
        var blueprint = new RESTBlueprint("test", "");

        // Assert
        Assert.NotNull(blueprint);
        Assert.Equal("", blueprint.Prefix);
    }

    #endregion

    #region AddRoutesOf Tests

    /// <summary>
    /// Test adding routes from a class
    /// </summary>
    [Fact]
    public void AddRoutesOf_WithValidRoutes_ShouldRegisterRoutes()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");

        // Act
        blueprint.AddRoutesOf<TestRoutes>();

        // Assert - Try to find the registered routes
        var args = new Dictionary<string, string>();

        Assert.True(blueprint.TryFindRoute(ERestMethod.Get, new[] { "test" }, args, out var getRoute));
        Assert.NotNull(getRoute);
        Assert.Equal(ERestMethod.Get, getRoute.RESTMethod);
        Assert.Equal("/test", getRoute.RESTEndpoint);

        Assert.True(blueprint.TryFindRoute(ERestMethod.Post, new[] { "users" }, args, out var postRoute));
        Assert.NotNull(postRoute);
        Assert.Equal(ERestMethod.Post, postRoute.RESTMethod);
    }

    /// <summary>
    /// Test adding routes with arguments
    /// </summary>
    [Fact]
    public void AddRoutesOf_WithArgumentRoutes_ShouldRegisterAndExtractArguments()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(ERestMethod.Get, new[] { "users", "123" }, args, out var route);

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal("/users/<id>", route.RESTEndpoint);
        Assert.True(args.ContainsKey("id"));
        Assert.Equal("123", args["id"]);
    }

    /// <summary>
    /// Test adding routes with multiple arguments
    /// </summary>
    [Fact]
    public void AddRoutesOf_WithMultipleArguments_ShouldExtractAll()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Put,
            new[] { "users", "456", "settings", "789" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal("/users/<id>/settings/<settingId>", route.RESTEndpoint);
        Assert.Equal(2, args.Count);
        Assert.Equal("456", args["id"]);
        Assert.Equal("789", args["settingId"]);
    }

    /// <summary>
    /// Test adding routes with URL encoding
    /// </summary>
    [Fact]
    public void AddRoutesOf_WithEncodedArguments_ShouldDecodeValues()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "users", "test%20user" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.Equal("test user", args["id"]);
    }

    #endregion

    #region TryFindRoute Tests

    /// <summary>
    /// Test finding a route that doesn't exist
    /// </summary>
    [Fact]
    public void TryFindRoute_WithNonExistentRoute_ShouldReturnFalse()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "nonexistent" },
            args,
            out var route
        );

        // Assert
        Assert.False(found);
        Assert.Null(route);
    }

    /// <summary>
    /// Test finding a route with wrong method
    /// </summary>
    [Fact]
    public void TryFindRoute_WithWrongMethod_ShouldReturnFalse()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Delete,  // Wrong method, should be Get
            new[] { "test" },
            args,
            out var route
        );

        // Assert
        Assert.False(found);
        Assert.Null(route);
    }

    /// <summary>
    /// Test finding a route with wrong segment count
    /// </summary>
    [Fact]
    public void TryFindRoute_WithWrongSegmentCount_ShouldReturnFalse()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "users", "123", "extra" },  // Too many segments
            args,
            out var route
        );

        // Assert
        Assert.False(found);
        Assert.Null(route);
    }

    #endregion

    #region AddBlueprint Tests

    /// <summary>
    /// Test adding a sub-blueprint
    /// </summary>
    [Fact]
    public void AddBlueprint_WithValidBlueprint_ShouldRegister()
    {
        // Arrange
        var mainBlueprint = new RESTBlueprint("main", "api");
        var subBlueprint = new RESTBlueprint("sub", "v1");
        subBlueprint.AddRoutesOf<TestRoutes>();

        // Act
        mainBlueprint.AddBlueprint(subBlueprint);

        // Assert
        var args = new Dictionary<string, string>();
        var found = mainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "v1", "test" },
            args,
            out var route
        );

        Assert.True(found);
        Assert.NotNull(route);
    }

    /// <summary>
    /// Test adding null blueprint throws
    /// </summary>
    [Fact]
    public void AddBlueprint_WithNull_ShouldThrow()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => blueprint.AddBlueprint(null!));
    }

    /// <summary>
    /// Test adding same blueprint twice throws
    /// </summary>
    [Fact]
    public void AddBlueprint_WithDuplicateBlueprint_ShouldThrow()
    {
        // Arrange
        var mainBlueprint = new RESTBlueprint("main");
        var subBlueprint = new RESTBlueprint("sub");

        // Act
        mainBlueprint.AddBlueprint(subBlueprint);

        // Assert
        var ex = Assert.Throws<Exception>(() => mainBlueprint.AddBlueprint(subBlueprint));
        Assert.Contains("already registered", ex.Message);
    }

    #endregion

    #region Blueprint Type Tests

    /// <summary>
    /// Test BlueprintType property
    /// </summary>
    [Fact]
    public void BlueprintType_ShouldReturnCorrectType()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");

        // Act & Assert
        Assert.Equal(typeof(RESTBlueprint), blueprint.BlueprintType);
    }

    /// <summary>
    /// Test RouteType property
    /// </summary>
    [Fact]
    public void RouteType_ShouldReturnCorrectType()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");

        // Act & Assert
        Assert.Equal(typeof(RESTRoute), blueprint.RouteType);
    }

    #endregion

    #region Complex Routing Tests

    /// <summary>
    /// Test nested blueprints with overlapping paths
    /// </summary>
    [Fact]
    public void ComplexRouting_WithNestedBlueprintsAndOverlappingPaths_ShouldRouteCorrectly()
    {
        // Arrange
        var mainBlueprint = new RESTBlueprint("main");
        var apiV1Blueprint = new RESTBlueprint("api-v1", "api/v1");
        var apiV2Blueprint = new RESTBlueprint("api-v2", "api/v2");

        apiV1Blueprint.AddRoutesOf<TestRoutes>();
        apiV2Blueprint.AddRoutesOf<TestRoutes>();

        mainBlueprint.AddBlueprint(apiV1Blueprint);
        mainBlueprint.AddBlueprint(apiV2Blueprint);

        // Act & Assert - v1 routes
        var args1 = new Dictionary<string, string>();
        Assert.True(mainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "v1", "test" },
            args1,
            out var route1
        ));
        Assert.NotNull(route1);

        // Act & Assert - v2 routes
        var args2 = new Dictionary<string, string>();
        Assert.True(mainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "v2", "test" },
            args2,
            out var route2
        ));
        Assert.NotNull(route2);
    }

    /// <summary>
    /// Test that prefix-only blueprint can find sub-blueprint routes
    /// </summary>
    [Fact]
    public void ComplexRouting_WithPrefixOnlyBlueprint_ShouldFindSubRoutes()
    {
        // Arrange
        var rootBlueprint = new RESTBlueprint("root");
        var prefixedBlueprint = new RESTBlueprint("prefixed", "api");
        prefixedBlueprint.AddRoutesOf<TestRoutes>();

        rootBlueprint.AddBlueprint(prefixedBlueprint);

        // Act
        var args = new Dictionary<string, string>();
        var found = rootBlueprint.TryFindRoute(
            ERestMethod.Post,
            new[] { "api", "users" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal(ERestMethod.Post, route.RESTMethod);
    }

    /// <summary>
    /// Test multiple levels of nested blueprints
    /// </summary>
    [Fact]
    public void ComplexRouting_WithMultipleLevelsOfNesting_ShouldResolveCorrectly()
    {
        // Arrange
        var level0 = new RESTBlueprint("level0");
        var level1 = new RESTBlueprint("level1", "api");
        var level2 = new RESTBlueprint("level2", "v1");
        var level3 = new RESTBlueprint("level3", "admin");

        level3.AddRoutesOf<TestRoutes>();
        level2.AddBlueprint(level3);
        level1.AddBlueprint(level2);
        level0.AddBlueprint(level1);

        // Act
        var args = new Dictionary<string, string>();
        var found = level0.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "v1", "admin", "test" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Test empty segments array
    /// </summary>
    [Fact]
    public void TryFindRoute_WithEmptySegments_ShouldReturnFalse()
    {
        // Arrange
        var blueprint = new RESTBlueprint("test");
        blueprint.AddRoutesOf<TestRoutes>();

        // Act
        var args = new Dictionary<string, string>();
        var found = blueprint.TryFindRoute(
            ERestMethod.Get,
            Array.Empty<string>(),
            args,
            out var route
        );

        // Assert
        Assert.False(found);
    }

    /// <summary>
    /// Test that REST_METHOD_COUNT is correct
    /// </summary>
    [Fact]
    public void REST_METHOD_COUNT_ShouldMatchEnumValues()
    {
        // Arrange & Act
        var enumValues = Enum.GetValues<ERestMethod>();

        // Assert
        Assert.Equal(enumValues.Length, RESTBlueprint.REST_METHOD_COUNT);
    }

    #endregion
}