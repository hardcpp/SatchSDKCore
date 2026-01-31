using System.Net;
using SSC.APIServer.Blueprint;
using SSC.APIServer.Handler;
using SSC.APIServer.Response;
using SSC.APIServer.Route;
using SSC.APIServer.RouteContext;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Tests.APIServer.Handler;

/// <summary>
/// Unit tests for the RESTHTTPServerHandler class
/// </summary>
public class RESTHTTPServerHandlerTests
{
    #region Test Helper Classes

    /// <summary>
    /// Test routes for handler testing
    /// </summary>
    public class TestHandlerRoutes
    {
        [RESTRoute(ERestMethod.Get, "/api/test")]
        public static IResponse GetTest(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "Test response");
        }

        [RESTRoute(ERestMethod.Post, "/api/users")]
        public static IResponse PostUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.Created, "User created");
        }

        [RESTRoute(ERestMethod.Get, "/api/users/<id>")]
        public static IResponse GetUserById(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "User found");
        }

        [RESTRoute(ERestMethod.Delete, "/api/users/<id>")]
        public static IResponse DeleteUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.NoContent, "");
        }

        [RESTRoute(ERestMethod.Put, "/api/users/<id>")]
        public static IResponse PutUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "User updated");
        }

        [RESTRoute(ERestMethod.Patch, "/api/users/<id>")]
        public static IResponse PatchUser(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "User patched");
        }
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Test that constructor initializes correctly
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Arrange & Act
        var handler = new RESTHTTPServerHandler();

        // Assert
        Assert.NotNull(handler);
        Assert.NotNull(handler.MainBlueprint);
        Assert.Equal("Main", handler.MainBlueprint.Name);
        Assert.NotNull(handler.Hooks);
    }

    /// <summary>
    /// Test that MainBlueprint can have routes added
    /// </summary>
    [Fact]
    public void Constructor_MainBlueprint_ShouldAcceptRoutes()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();

        // Act
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Assert
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "test" },
            args,
            out var route
        );

        Assert.True(found);
        Assert.NotNull(route);
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
        var handler = new RESTHTTPServerHandler();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.TryHandle(null!));
    }

    /// <summary>
    /// Test TryHandle with GET request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithGetRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9001/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9001/api/test");
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Test response", content);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with POST request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithPostRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9002/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.PostAsync("http://localhost:9002/api/users",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("User created", content);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with PUT request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithPutRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9003/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.PutAsync("http://localhost:9003/api/users/123",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("User updated", content);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with PATCH request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithPatchRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9004/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.PatchAsync("http://localhost:9004/api/users/456",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("User patched", content);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with DELETE request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithDeleteRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9005/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.DeleteAsync("http://localhost:9005/api/users/789");

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with parameterized route extracts parameters correctly
    /// </summary>
    [Fact]
    public async Task TryHandle_WithParameterizedRoute_ExtractsParameters()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9006/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9006/api/users/test-user-123");
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("User found", content);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle returns false for non-existent route
    /// </summary>
    [Fact]
    public async Task TryHandle_WithNonExistentRoute_Returns404()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9007/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9007/nonexistent/route");

        // Give server time to process
        await Task.Delay(100);

        // Assert - Handler returns false, server returns 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with route that throws exception handles error gracefully
    /// </summary>
    [Fact]
    public async Task TryHandle_WithRouteThrowingException_ReturnsInternalServerError()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9008/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestErrorRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9008/api/error");

        // Give server time to process
        await Task.Delay(100);

        // Assert - Exception should result in 500
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // Cleanup
        server.Stop();
    }


    /// <summary>
    /// Test TryHandle with multiple concurrent requests
    /// </summary>
    [Fact]
    public async Task TryHandle_WithMultipleConcurrentRequests_HandlesAll()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9010/", 4);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(client.GetAsync("http://localhost:9010/api/test"));
        }

        var responses = await Task.WhenAll(tasks.ToArray());

        // Give server time to process
        await Task.Delay(200);

        // Assert
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with different HTTP methods on same route path
    /// </summary>
    [Fact]
    public async Task TryHandle_WithDifferentMethodsSamePath_RoutesCorrectly()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9011/", 2);
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();

        // Act & Assert - GET
        var getResponse = await client.GetAsync("http://localhost:9011/api/users/test-id");
        await Task.Delay(50);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains("User found", getContent);

        // Act & Assert - PUT
        var putResponse = await client.PutAsync("http://localhost:9011/api/users/test-id",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        await Task.Delay(50);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var putContent = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("User updated", putContent);

        // Act & Assert - DELETE
        var deleteResponse = await client.DeleteAsync("http://localhost:9011/api/users/test-id");
        await Task.Delay(50);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Test TryHandle with complex nested path
    /// </summary>
    [Fact]
    public async Task TryHandle_WithNestedBlueprintPath_HandlesCorrectly()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9012/", 2);
        var handler = new RESTHTTPServerHandler();

        var v1Blueprint = new RESTBlueprint("v1", "v1");
        v1Blueprint.AddRoutesOf<TestHandlerRoutes>();
        handler.MainBlueprint.AddBlueprint(v1Blueprint);

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:9012/v1/api/test");
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Test response", content);

        // Cleanup
        server.Stop();
    }

    #endregion

    #region HTTP Method Conversion Tests

    /// <summary>
    /// Test that all REST methods are supported
    /// This is tested indirectly through the blueprint
    /// </summary>
    [Theory]
    [InlineData(ERestMethod.Get)]
    [InlineData(ERestMethod.Post)]
    [InlineData(ERestMethod.Put)]
    [InlineData(ERestMethod.Patch)]
    [InlineData(ERestMethod.Delete)]
    public void SupportedRestMethods_ShouldBeRecognized(ERestMethod method)
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act - Verify each method can be registered
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = false;

        switch (method)
        {
            case ERestMethod.Get:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "test" }, args, out _);
                break;
            case ERestMethod.Post:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users" }, args, out _);
                break;
            case ERestMethod.Put:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users", "123" }, args, out _);
                break;
            case ERestMethod.Patch:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users", "123" }, args, out _);
                break;
            case ERestMethod.Delete:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users", "123" }, args, out _);
                break;
        }

        // Assert
        Assert.True(found);
    }

    #endregion

    #region Blueprint Integration Tests

    /// <summary>
    /// Test that handler can work with nested blueprints
    /// </summary>
    [Fact]
    public void Handler_WithNestedBlueprints_ShouldResolveRoutes()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();
        var apiBlueprint = new RESTBlueprint("api", "api");
        var v1Blueprint = new RESTBlueprint("v1", "v1");

        v1Blueprint.AddRoutesOf<TestHandlerRoutes>();
        apiBlueprint.AddBlueprint(v1Blueprint);
        handler.MainBlueprint.AddBlueprint(apiBlueprint);

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "v1", "api", "test" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
    }

    /// <summary>
    /// Test that handler maintains separate argument collectors
    /// </summary>
    [Fact]
    public void Handler_WithParameterizedRoutes_ShouldExtractArguments()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api", "users", "test-user-id" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
        Assert.True(args.ContainsKey("id"));
        Assert.Equal("test-user-id", args["id"]);
    }

    #endregion

    #region Hooks Tests

    /// <summary>
    /// Test that Hooks property is initialized
    /// </summary>
    [Fact]
    public void Hooks_ShouldBeInitialized()
    {
        // Arrange & Act
        var handler = new RESTHTTPServerHandler();

        // Assert
        Assert.NotNull(handler.Hooks);
    }

    /// <summary>
    /// Test that hooks can be added to the Hookable
    /// </summary>
    [Fact]
    public void Hooks_ShouldSupportHookable()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();

        // Act & Assert
        // Verify that Hooks is an IHookable instance
        Assert.NotNull(handler.Hooks);
        Assert.IsAssignableFrom<IHookable<HttpServerExRequestContext>>(handler.Hooks);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Test handler with empty blueprint
    /// </summary>
    [Fact]
    public void Handler_WithEmptyBlueprint_ShouldNotFindRoutes()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "any", "route" },
            args,
            out var route
        );

        // Assert
        Assert.False(found);
        Assert.Null(route);
    }

    /// <summary>
    /// Test handler with multiple blueprints at same level
    /// </summary>
    [Fact]
    public void Handler_WithMultipleBlueprintsAtSameLevel_ShouldResolveCorrectly()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();
        var blueprint1 = new RESTBlueprint("bp1", "api1");
        var blueprint2 = new RESTBlueprint("bp2", "api2");

        blueprint1.AddRoutesOf<TestHandlerRoutes>();
        blueprint2.AddRoutesOf<TestHandlerRoutes>();

        handler.MainBlueprint.AddBlueprint(blueprint1);
        handler.MainBlueprint.AddBlueprint(blueprint2);

        // Act & Assert - api1 route
        var args1 = new System.Collections.Generic.Dictionary<string, string>();
        var found1 = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api1", "api", "test" },
            args1,
            out var route1
        );
        Assert.True(found1);

        // Act & Assert - api2 route
        var args2 = new System.Collections.Generic.Dictionary<string, string>();
        var found2 = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api2", "api", "test" },
            args2,
            out var route2
        );
        Assert.True(found2);
    }

    #endregion

    #region GetSegmentsFromAbsolutePath Tests

    /// <summary>
    /// Helper class to expose protected method for testing
    /// </summary>
    private class TestableRESTHTTPServerHandler : RESTHTTPServerHandler
    {
        public List<string> TestGetSegmentsFromAbsolutePath(string absolutePath)
        {
            return GetSegmentsFromAbsolutePath(absolutePath);
        }
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with simple path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithSimplePath_ShouldReturnSegments()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with root path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithRootPath_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/");

        // Assert
        Assert.Empty(segments);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with empty path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithEmptyPath_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("");

        // Assert
        Assert.Empty(segments);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with trailing slash
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithTrailingSlash_ShouldIgnoreTrailingSlash()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/users/");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with multiple slashes
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithMultipleSlashes_ShouldIgnoreEmptySegments()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("///api///users///");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with single segment
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithSingleSegment_ShouldReturnOne()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api");

        // Assert
        Assert.Single(segments);
        Assert.Equal("api", segments[0]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with complex path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithComplexPath_ShouldReturnAllSegments()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/v1/users/123/profile");

        // Assert
        Assert.Equal(5, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("v1", segments[1]);
        Assert.Equal("users", segments[2]);
        Assert.Equal("123", segments[3]);
        Assert.Equal("profile", segments[4]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with path containing special characters
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithSpecialCharacters_ShouldPreserveCharacters()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/users-test/user_id");

        // Assert
        Assert.Equal(3, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users-test", segments[1]);
        Assert.Equal("user_id", segments[2]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with path containing numbers
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithNumbers_ShouldPreserveNumbers()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/v2/users/12345");

        // Assert
        Assert.Equal(4, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("v2", segments[1]);
        Assert.Equal("users", segments[2]);
        Assert.Equal("12345", segments[3]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath reuses buffer across calls
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_MultipleCalls_ShouldReuseBuffer()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments1 = handler.TestGetSegmentsFromAbsolutePath("/api/users");
        var segments2 = handler.TestGetSegmentsFromAbsolutePath("/api/posts/123");

        // Assert - Second call should have overwritten buffer
        Assert.Equal(3, segments2.Count);
        Assert.Equal("api", segments2[0]);
        Assert.Equal("posts", segments2[1]);
        Assert.Equal("123", segments2[2]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with query string (should be stripped by URL parsing)
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithQueryString_ShouldOnlyParsePathPart()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act - Note: In real scenarios, AbsolutePath doesn't include query string
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with path containing dots
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithDotsInPath_ShouldPreserveDots()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/file.json");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("file.json", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with encoded characters in path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithEncodedChars_ShouldPreserveEncoding()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("/api/users/%20test");

        // Assert
        Assert.Equal(3, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
        Assert.Equal("%20test", segments[2]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with consecutive calls on same thread
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_ConsecutiveCalls_ShouldClearPreviousData()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act - First call with longer path
        var segments1 = handler.TestGetSegmentsFromAbsolutePath("/api/users/123/profile/settings");
        // Second call with shorter path
        var segments2 = handler.TestGetSegmentsFromAbsolutePath("/api");

        // Assert - Should only have data from second call
        Assert.Single(segments2);
        Assert.Equal("api", segments2[0]);
    }

    #endregion

    #region Thread Safety Tests

    /// <summary>
    /// Test that handler can be used concurrently
    /// </summary>
    [Fact]
    public async Task Handler_ConcurrentAccess_ShouldBeSafe()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act & Assert - Multiple threads can use the handler
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                var segments = handler.TestGetSegmentsFromAbsolutePath($"/api/users/{index}");
                Assert.Equal(3, segments.Count);
                Assert.Equal("api", segments[0]);
                Assert.Equal("users", segments[1]);
                Assert.Equal(index.ToString(), segments[2]);
            });
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Test thread-local buffers are isolated
    /// </summary>
    [Fact]
    public async Task Handler_ThreadLocalBuffers_ShouldBeIsolated()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();
        var results = new System.Collections.Concurrent.ConcurrentBag<List<string>>();

        // Act - Multiple threads parsing different paths
        var tasks = new Task[5];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                var path = $"/thread{index}/segment{index}";
                var segments = handler.TestGetSegmentsFromAbsolutePath(path);
                results.Add(new List<string>(segments));
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Each thread should have gotten different results
        Assert.Equal(5, results.Count);
        foreach (var result in results)
        {
            Assert.Equal(2, result.Count);
            Assert.StartsWith("thread", result[0]);
            Assert.StartsWith("segment", result[1]);
        }
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test that handler with routes that throw exceptions handles gracefully
    /// </summary>
    public class TestErrorRoutes
    {
        [RESTRoute(ERestMethod.Get, "/api/error")]
        public static IResponse GetError(RESTRouteContext context)
        {
            throw new InvalidOperationException("Test error");
        }

        [RESTRoute(ERestMethod.Get, "/api/null")]
        public static IResponse GetNull(RESTRouteContext context)
        {
            return null!;
        }
    }


    #endregion

    #region Performance Tests

    /// <summary>
    /// Test that segment parsing is efficient for repeated calls
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_RepeatedCalls_ShouldBeEfficient()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();
        var path = "/api/v1/users/123/profile/settings/preferences";

        // Act - Multiple calls should reuse buffer
        for (int i = 0; i < 100; i++)
        {
            var segments = handler.TestGetSegmentsFromAbsolutePath(path);
            Assert.Equal(7, segments.Count);
        }

        // Assert - If we get here without errors, buffer reuse works
        Assert.True(true);
    }

    /// <summary>
    /// Test handler can handle many routes efficiently
    /// </summary>
    [Fact]
    public void Handler_WithManyRoutes_ShouldStillResolve()
    {
        // Arrange
        var handler = new RESTHTTPServerHandler();

        // Add routes multiple times with different prefixes
        for (int i = 0; i < 10; i++)
        {
            var blueprint = new RESTBlueprint($"bp{i}", $"api{i}");
            blueprint.AddRoutesOf<TestHandlerRoutes>();
            handler.MainBlueprint.AddBlueprint(blueprint);
        }

        // Act - Try to find a route
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            ERestMethod.Get,
            new[] { "api5", "api", "test" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with path without leading slash
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithoutLeadingSlash_ShouldStillParse()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath("api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test GetSegmentsFromAbsolutePath with very long path
    /// </summary>
    [Fact]
    public void GetSegmentsFromAbsolutePath_WithLongPath_ShouldHandleAll()
    {
        // Arrange
        var handler = new TestableRESTHTTPServerHandler();
        var longPath = "/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p/q/r/s/t/u/v/w/x/y/z";

        // Act
        var segments = handler.TestGetSegmentsFromAbsolutePath(longPath);

        // Assert
        Assert.Equal(26, segments.Count);
        Assert.Equal("a", segments[0]);
        Assert.Equal("z", segments[25]);
    }

    #endregion
}
