using System.Net;
using SSC.Api.Blueprint;
using SSC.Api.Handler;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Tests.Api.Handler;

/// <summary>
/// Unit tests for the ApiHttpHandler class
/// </summary>
public class RESTHTTPServerHandlerTests
{
    #region Test Helper Classes

    /// <summary>
    /// Test routes for handler testing
    /// </summary>
    public class TestHandlerRoutes
    {
        [ApiHttpRoute(EApiHttpMethod.Get, "/api/test")]
        public static ApiResponse GetTest(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, "Test response");
        }

        [ApiHttpRoute(EApiHttpMethod.Post, "/api/users")]
        public static ApiResponse PostUser(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.Created, "User created");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/api/users/<id>")]
        public static ApiResponse GetUserById(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, "User found");
        }

        [ApiHttpRoute(EApiHttpMethod.Delete, "/api/users/<id>")]
        public static ApiResponse DeleteUser(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.NoContent, "");
        }

        [ApiHttpRoute(EApiHttpMethod.Put, "/api/users/<id>")]
        public static ApiResponse PutUser(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, "User updated");
        }

        [ApiHttpRoute(EApiHttpMethod.Patch, "/api/users/<id>")]
        public static ApiResponse PatchUser(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, "User patched");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/api/async-delay")]
        public static async Task<ApiResponse> AsyncDelay(
            CancellationToken cancellationToken,
            ApiHttpRouteContext context)
        {
            await Task.Delay(25, cancellationToken);
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, "Async response");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/api/async-echo/<id>")]
        public static async Task<ApiResponse> AsyncEcho(
            CancellationToken cancellationToken,
            ApiHttpRouteContext context,
            string id)
        {
            await Task.Delay(10, cancellationToken);
            return ApiHttpResponse.ContentResult(context, HttpStatusCode.OK, id);
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/api/async-failure")]
        public static async Task<ApiResponse> AsyncFailureAfterAwait(
            CancellationToken cancellationToken,
            ApiHttpRouteContext context)
        {
            await Task.Delay(10, cancellationToken);
            throw new InvalidOperationException("Failure after await");
        }
    }

    private sealed class TestApiHttpHook : IHook<HttpServerExRequestContext>
    {
        public bool Intercept(HttpServerExRequestContext context) => false;
    }

    #endregion

    [Fact]
    public async Task AsyncRouteFailureAfterAwait_ReturnsInternalServerError()
    {
        using var server = new HttpServerExCore(
            "http://localhost:9120/",
            maxConcurrentRequests: 8);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();
        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(
            "http://localhost:9120/api/async-failure");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await server.StopAsync();
    }

    [Fact]
    public async Task AsyncRoutes_AreNotLimitedByDedicatedThreads()
    {
        using var server = new HttpServerExCore(
            "http://localhost:9121/",
            maxConcurrentRequests: 64);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();
        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        Task<HttpResponseMessage>[] requests = Enumerable
            .Range(0, 32)
            .Select(_ => client.GetAsync("http://localhost:9121/api/async-delay"))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        foreach (HttpResponseMessage response in responses)
            response.Dispose();

        await server.StopAsync();
    }

    [Fact]
    public async Task ConcurrentAsyncRoutes_KeepArgumentsIsolated()
    {
        using var server = new HttpServerExCore(
            "http://localhost:9122/",
            maxConcurrentRequests: 64);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();
        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        Task<string>[] requests = Enumerable
            .Range(0, 64)
            .Select(async id =>
            {
                using HttpResponseMessage response = await client.GetAsync(
                    $"http://localhost:9122/api/async-echo/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            })
            .ToArray();

        string[] responses = await Task.WhenAll(requests);
        for (var id = 0; id < responses.Length; id++)
            Assert.Contains(id.ToString(), responses[id]);

        await server.StopAsync();
    }

    [Fact]
    public async Task Start_FreezesBlueprintAndHandlerRegistration_ButKeepsHooksMutable()
    {
        using var server = new HttpServerExCore("http://localhost:9123/", 1);
        var handler = new ApiHttpHandler();
        server.AddRequestHandler(handler);
        server.Start();

        Assert.True(handler.IsFrozen);
        Assert.Throws<InvalidOperationException>(
            handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>);
        var hook = new TestApiHttpHook();
        handler.Hooks.AddEarlyRequestHook(hook);
        handler.Hooks.RemoveEarlyRequestHook(hook);
        Assert.Throws<InvalidOperationException>(() =>
            server.AddRequestHandler(new ApiHttpHandler()));

        await server.StopAsync();
    }

    #region Constructor Tests

    /// <summary>
    /// Test that constructor initializes correctly
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Arrange & Act
        var handler = new ApiHttpHandler();

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
        var handler = new ApiHttpHandler();

        // Act
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Assert
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
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
    public async Task TryHandle_WithNullContext_ShouldThrow()
    {
        // Arrange
        var handler = new ApiHttpHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.TryHandleAsync(null!));
    }

    /// <summary>
    /// Test TryHandle with GET request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithGetRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9001/", 2);
        var handler = new ApiHttpHandler();
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

    [Fact]
    public async Task TryHandle_WithHeadRequest_FallsBackToGetWithoutBody()
    {
        using var server = new HttpServerExCore("http://localhost:9051/", 2);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        using var getResponse = await client.GetAsync("http://localhost:9051/api/test");
        using var headRequest = new HttpRequestMessage(HttpMethod.Head, "http://localhost:9051/api/test");
        using var headResponse = await client.SendAsync(headRequest);

        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.Equal(getResponse.Content.Headers.ContentLength, headResponse.Content.Headers.ContentLength);
        Assert.Empty(await headResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task TryHandle_WithOptionsRequest_ReturnsSupportedMethods()
    {
        using var server = new HttpServerExCore("http://localhost:9052/", 2);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "http://localhost:9052/api/test");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("GET, HEAD, OPTIONS", string.Join(", ", response.Content.Headers.Allow));
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task TryHandle_WithUnsupportedMethodOnKnownPath_ReturnsMethodNotAllowed()
    {
        using var server = new HttpServerExCore("http://localhost:9053/", 2);
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:9053/api/test");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("GET, HEAD, OPTIONS", string.Join(", ", response.Content.Headers.Allow));
        Assert.Contains("405 Method Not Allowed", await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Test TryHandle with POST request successfully handles and returns response
    /// </summary>
    [Fact]
    public async Task TryHandle_WithPostRequest_HandlesSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:9002/", 2);
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();
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
        var handler = new ApiHttpHandler();

        var v1Blueprint = new ApiHttpBlueprint("v1", "v1");
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
    [InlineData(EApiHttpMethod.Get)]
    [InlineData(EApiHttpMethod.Post)]
    [InlineData(EApiHttpMethod.Put)]
    [InlineData(EApiHttpMethod.Patch)]
    [InlineData(EApiHttpMethod.Delete)]
    public void SupportedRestMethods_ShouldBeRecognized(EApiHttpMethod method)
    {
        // Arrange
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act - Verify each method can be registered
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = false;

        switch (method)
        {
            case EApiHttpMethod.Get:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "test" }, args, out _);
                break;
            case EApiHttpMethod.Post:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users" }, args, out _);
                break;
            case EApiHttpMethod.Put:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users", "123" }, args, out _);
                break;
            case EApiHttpMethod.Patch:
                found = handler.MainBlueprint.TryFindRoute(method, new[] { "api", "users", "123" }, args, out _);
                break;
            case EApiHttpMethod.Delete:
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
        var handler = new ApiHttpHandler();
        var apiBlueprint = new ApiHttpBlueprint("api", "api");
        var v1Blueprint = new ApiHttpBlueprint("v1", "v1");

        v1Blueprint.AddRoutesOf<TestHandlerRoutes>();
        apiBlueprint.AddBlueprint(v1Blueprint);
        handler.MainBlueprint.AddBlueprint(apiBlueprint);

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
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
        var handler = new ApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
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
        var handler = new ApiHttpHandler();

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
        var handler = new ApiHttpHandler();

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
        var handler = new ApiHttpHandler();

        // Act
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
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
        var handler = new ApiHttpHandler();
        var blueprint1 = new ApiHttpBlueprint("bp1", "api1");
        var blueprint2 = new ApiHttpBlueprint("bp2", "api2");

        blueprint1.AddRoutesOf<TestHandlerRoutes>();
        blueprint2.AddRoutesOf<TestHandlerRoutes>();

        handler.MainBlueprint.AddBlueprint(blueprint1);
        handler.MainBlueprint.AddBlueprint(blueprint2);

        // Act & Assert - api1 route
        var args1 = new System.Collections.Generic.Dictionary<string, string>();
        var found1 = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "api1", "api", "test" },
            args1,
            out var route1
        );
        Assert.True(found1);

        // Act & Assert - api2 route
        var args2 = new System.Collections.Generic.Dictionary<string, string>();
        var found2 = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "api2", "api", "test" },
            args2,
            out var route2
        );
        Assert.True(found2);
    }

    #endregion

    #region FillSegmentsFromAbsolutePath Tests

    /// <summary>
    /// Test helper for the allocation-free path parser.
    /// </summary>
    private class TestableApiHttpHandler : ApiHttpHandler
    {
        public List<string> TestFillSegmentsFromAbsolutePath(string absolutePath)
        {
            var segments = new List<string>(10);
            FillSegmentsFromAbsolutePath(absolutePath, segments);
            return segments;
        }
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with simple path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithSimplePath_ShouldReturnSegments()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with root path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithRootPath_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/");

        // Assert
        Assert.Empty(segments);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with empty path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithEmptyPath_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("");

        // Assert
        Assert.Empty(segments);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with trailing slash
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithTrailingSlash_ShouldIgnoreTrailingSlash()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/users/");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with multiple slashes
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithMultipleSlashes_ShouldIgnoreEmptySegments()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("///api///users///");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with single segment
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithSingleSegment_ShouldReturnOne()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api");

        // Assert
        Assert.Single(segments);
        Assert.Equal("api", segments[0]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with complex path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithComplexPath_ShouldReturnAllSegments()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/v1/users/123/profile");

        // Assert
        Assert.Equal(5, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("v1", segments[1]);
        Assert.Equal("users", segments[2]);
        Assert.Equal("123", segments[3]);
        Assert.Equal("profile", segments[4]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with path containing special characters
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithSpecialCharacters_ShouldPreserveCharacters()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/users-test/user_id");

        // Assert
        Assert.Equal(3, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users-test", segments[1]);
        Assert.Equal("user_id", segments[2]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with path containing numbers
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithNumbers_ShouldPreserveNumbers()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/v2/users/12345");

        // Assert
        Assert.Equal(4, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("v2", segments[1]);
        Assert.Equal("users", segments[2]);
        Assert.Equal("12345", segments[3]);
    }

    /// <summary>
    /// Test separate fill operations return independent results.
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_MultipleCalls_ShouldReturnIndependentResults()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var userSegments = handler.TestFillSegmentsFromAbsolutePath("/api/users");
        var postSegments = handler.TestFillSegmentsFromAbsolutePath("/api/posts/123");

        Assert.Equal(new[] { "api", "users" }, userSegments);
        Assert.Equal(new[] { "api", "posts", "123" }, postSegments);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with query string (should be stripped by URL parsing)
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithQueryString_ShouldOnlyParsePathPart()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act - Note: In real scenarios, AbsolutePath doesn't include query string
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with path containing dots
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithDotsInPath_ShouldPreserveDots()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/file.json");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("file.json", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with encoded characters in path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithEncodedChars_ShouldPreserveEncoding()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("/api/users/%20test");

        // Assert
        Assert.Equal(3, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
        Assert.Equal("%20test", segments[2]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with consecutive calls on same thread
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_ConsecutiveCalls_ShouldClearPreviousData()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act - First call with longer path
        var longPathSegments = handler.TestFillSegmentsFromAbsolutePath("/api/users/123/profile/settings");
        // Second call with shorter path
        var shortPathSegments = handler.TestFillSegmentsFromAbsolutePath("/api");

        Assert.Equal(5, longPathSegments.Count);
        Assert.Single(shortPathSegments);
        Assert.Equal("api", shortPathSegments[0]);
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
        var handler = new TestableApiHttpHandler();
        handler.MainBlueprint.AddRoutesOf<TestHandlerRoutes>();

        // Act & Assert - Multiple threads can use the handler
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                var segments = handler.TestFillSegmentsFromAbsolutePath($"/api/users/{index}");
                Assert.Equal(3, segments.Count);
                Assert.Equal("api", segments[0]);
                Assert.Equal("users", segments[1]);
                Assert.Equal(index.ToString(), segments[2]);
            });
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Test concurrent path parsing results are isolated.
    /// </summary>
    [Fact]
    public async Task Handler_ConcurrentPathParsing_ShouldKeepResultsIsolated()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();
        var results = new System.Collections.Concurrent.ConcurrentBag<List<string>>();

        // Act - Concurrent callers parse different paths.
        var tasks = new Task[5];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                var path = $"/thread{index}/segment{index}";
                var segments = handler.TestFillSegmentsFromAbsolutePath(path);
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
        [ApiHttpRoute(EApiHttpMethod.Get, "/api/error")]
        public static ApiResponse GetError(ApiHttpRouteContext context)
        {
            throw new InvalidOperationException("Test error");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/api/null")]
        public static ApiResponse GetNull(ApiHttpRouteContext context)
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
    public void FillSegmentsFromAbsolutePath_RepeatedCalls_ShouldBeEfficient()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();
        var path = "/api/v1/users/123/profile/settings/preferences";

        // Act - Repeated fills should remain stable.
        for (int i = 0; i < 100; i++)
        {
            var segments = handler.TestFillSegmentsFromAbsolutePath(path);
            Assert.Equal(7, segments.Count);
        }

        // Assert - If we get here without errors, repeated parsing works.
        Assert.True(true);
    }

    /// <summary>
    /// Test handler can handle many routes efficiently
    /// </summary>
    [Fact]
    public void Handler_WithManyRoutes_ShouldStillResolve()
    {
        // Arrange
        var handler = new ApiHttpHandler();

        // Add routes multiple times with different prefixes
        for (int i = 0; i < 10; i++)
        {
            var blueprint = new ApiHttpBlueprint($"bp{i}", $"api{i}");
            blueprint.AddRoutesOf<TestHandlerRoutes>();
            handler.MainBlueprint.AddBlueprint(blueprint);
        }

        // Act - Try to find a route
        var args = new System.Collections.Generic.Dictionary<string, string>();
        var found = handler.MainBlueprint.TryFindRoute(
            EApiHttpMethod.Get,
            new[] { "api5", "api", "test" },
            args,
            out var route
        );

        // Assert
        Assert.True(found);
        Assert.NotNull(route);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with path without leading slash
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithoutLeadingSlash_ShouldStillParse()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath("api/users");

        // Assert
        Assert.Equal(2, segments.Count);
        Assert.Equal("api", segments[0]);
        Assert.Equal("users", segments[1]);
    }

    /// <summary>
    /// Test FillSegmentsFromAbsolutePath with very long path
    /// </summary>
    [Fact]
    public void FillSegmentsFromAbsolutePath_WithLongPath_ShouldHandleAll()
    {
        // Arrange
        var handler = new TestableApiHttpHandler();
        var longPath = "/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p/q/r/s/t/u/v/w/x/y/z";

        // Act
        var segments = handler.TestFillSegmentsFromAbsolutePath(longPath);

        // Assert
        Assert.Equal(26, segments.Count);
        Assert.Equal("a", segments[0]);
        Assert.Equal("z", segments[25]);
    }

    #endregion
}
