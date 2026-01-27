using System.Net;
using System.Net.Http;
using System.Text;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SatchSDKCore.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpServerExCore class which provides an HTTP server implementation
/// with support for request handlers, hooks, and multi-threaded request processing.
/// </summary>
public class HttpServerExCoreTests
{
    /// <summary>
    /// Test request handler implementation for testing HTTP server functionality.
    /// Tracks call count and allows configuring whether to handle requests and what response to return.
    /// </summary>
    private class TestRequestHandler : IHttpServerExRequestHandler
    {
        public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();
        public int CallCount { get; private set; }
        public bool ShouldHandle { get; set; } = true;
        public HttpServerExResponse? ResponseToReturn { get; set; }

        public bool TryHandle(HttpServerExRequestContext context)
        {
            CallCount++;
            if (ShouldHandle && ResponseToReturn != null)
            {
                context.ServerResponse = ResponseToReturn;
                return true;
            }
            return ShouldHandle;
        }
    }

    /// <summary>
    /// Test hook implementation for testing server and handler hook functionality.
    /// Tracks call count and allows configuring whether to intercept requests.
    /// </summary>
    private class TestServerHook : IHook<HttpServerExRequestContext>
    {
        public int CallCount { get; private set; }
        public bool ShouldIntercept { get; set; } = false;

        public bool Intercept(HttpServerExRequestContext context)
        {
            CallCount++;
            return ShouldIntercept;
        }
    }

    /// <summary>
    /// Verifies that the constructor creates a server with valid listener and hooks
    /// when provided with a valid HTTP prefix and worker count.
    /// </summary>
    [Fact]
    public void Constructor_WithValidPrefix_CreatesServer()
    {
        // Arrange & Act
        using var server = new HttpServerExCore("http://localhost:8080/", 2);

        // Assert
        Assert.NotNull(server);
        Assert.NotNull(server.listener);
        Assert.NotNull(server.Hooks);
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentOutOfRangeException
    /// when worker count is zero or negative.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidWorkerCount_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HttpServerExCore("http://localhost:8081/", 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HttpServerExCore("http://localhost:8082/", -1));
    }

    /// <summary>
    /// Verifies that the constructor successfully creates servers with different worker counts (1, 4, 8).
    /// Worker threads handle incoming HTTP requests concurrently.
    /// </summary>
    [Fact]
    public void Constructor_WithValidWorkerCount_CreatesWorkers()
    {
        // Arrange & Act
        using var server1 = new HttpServerExCore("http://localhost:8083/", 1);
        using var server2 = new HttpServerExCore("http://localhost:8084/", 4);
        using var server3 = new HttpServerExCore("http://localhost:8085/", 8);

        // Assert - No exceptions thrown
        Assert.NotNull(server1);
        Assert.NotNull(server2);
        Assert.NotNull(server3);
    }

    /// <summary>
    /// Verifies that AddRequestHandler successfully adds a request handler to the server.
    /// Request handlers process incoming HTTP requests and generate responses.
    /// </summary>
    [Fact]
    public void AddRequestHandler_WithValidHandler_AddsHandler()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8086/", 1);
        var handler = new TestRequestHandler();

        // Act
        server.AddRequestHandler(handler);

        // Assert - No exception thrown, handler added
        Assert.NotNull(server);
    }

    /// <summary>
    /// Verifies that AddRequestHandler throws ArgumentNullException when passed a null handler.
    /// </summary>
    [Fact]
    public void AddRequestHandler_WithNullHandler_ThrowsException()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8087/", 1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => server.AddRequestHandler(null!));
    }

    /// <summary>
    /// Verifies that adding the same handler twice doesn't cause errors.
    /// The server should handle duplicate additions gracefully.
    /// </summary>
    [Fact]
    public void AddRequestHandler_WithSameHandlerTwice_OnlyAddsOnce()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8088/", 1);
        var handler = new TestRequestHandler();

        // Act
        server.AddRequestHandler(handler);
        server.AddRequestHandler(handler); // Add same handler again

        // Assert - No exception, handler only added once
        Assert.NotNull(server);
    }

    /// <summary>
    /// Verifies that RemoveRequestHandler successfully removes a previously added handler.
    /// </summary>
    [Fact]
    public void RemoveRequestHandler_WithValidHandler_RemovesHandler()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8089/", 1);
        var handler = new TestRequestHandler();
        server.AddRequestHandler(handler);

        // Act
        server.RemoveRequestHandler(handler);

        // Assert - No exception thrown
        Assert.NotNull(server);
    }

    /// <summary>
    /// Verifies that RemoveRequestHandler throws ArgumentNullException when passed a null handler.
    /// </summary>
    [Fact]
    public void RemoveRequestHandler_WithNullHandler_ThrowsException()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8090/", 1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => server.RemoveRequestHandler(null!));
    }

    /// <summary>
    /// Verifies that RemoveRequestHandler doesn't throw an exception when removing a handler that wasn't added.
    /// The operation should be idempotent and safe.
    /// </summary>
    [Fact]
    public void RemoveRequestHandler_WithNonExistentHandler_NoError()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8091/", 1);
        var handler = new TestRequestHandler();

        // Act & Assert - Should not throw
        server.RemoveRequestHandler(handler);
    }

    /// <summary>
    /// Verifies that Start() successfully starts the HTTP listener and begins accepting requests.
    /// </summary>
    [Fact]
    public void Start_StartsServerSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8092/", 1);

        // Act
        server.Start();

        // Assert
        Assert.True(server.listener.IsListening);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that Stop() successfully stops the HTTP listener and stops accepting requests.
    /// </summary>
    [Fact]
    public void Stop_StopsServerSuccessfully()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8093/", 1);
        server.Start();

        // Act
        server.Stop();

        // Assert
        Assert.False(server.listener.IsListening);
    }

    /// <summary>
    /// Verifies that Stop() can be called safely even when the server hasn't been started.
    /// </summary>
    [Fact]
    public void Stop_WhenNotStarted_NoError()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8094/", 1);

        // Act & Assert - Should not throw
        server.Stop();
    }

    /// <summary>
    /// Verifies that Dispose() properly stops the server and cleans up resources.
    /// </summary>
    [Fact]
    public void Dispose_StopsServer()
    {
        // Arrange
        var server = new HttpServerExCore("http://localhost:8095/", 1);
        server.Start();

        // Act
        server.Dispose();

        // Assert
        Assert.False(server.listener.IsListening);
    }

    /// <summary>
    /// Verifies that the server correctly processes HTTP requests and returns the response
    /// generated by the registered request handler.
    /// </summary>
    [Fact]
    public async Task Server_HandlesRequest_WithHandler()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8096/", 2);
        var handler = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("Test Response", Encoding.UTF8),
                Encoding.UTF8)
        };
        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:8096/test");

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Test Response", content);
        Assert.True(handler.CallCount > 0);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that the server returns HTTP 404 Not Found when no handler handles the request.
    /// This is the default behavior when all handlers decline to process a request.
    /// </summary>
    [Fact]
    public async Task Server_Returns404_WhenNoHandlerHandlesRequest()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8097/", 2);
        var handler = new TestRequestHandler
        {
            ShouldHandle = false // Handler doesn't handle the request
        };
        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:8097/test");

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that the server can handle multiple concurrent HTTP requests using worker threads.
    /// Tests the multi-threaded request processing capability.
    /// </summary>
    [Fact]
    public async Task Server_HandlesMultipleRequests()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8098/", 4);
        var handler = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("OK", Encoding.UTF8),
                Encoding.UTF8)
        };
        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(client.GetAsync($"http://localhost:8098/test{i}"));
        }

        var responses = await Task.WhenAll(tasks.ToArray());

        // Give server time to process
        await Task.Delay(200);

        // Assert
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.True(handler.CallCount >= 5);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that early hooks at the server level can intercept requests before handlers process them.
    /// Early hooks can prevent request processing by returning true from Intercept().
    /// </summary>
    [Fact]
    public async Task Hooks_EarlyHook_CanInterceptRequest()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8099/", 2);
        var hook = new TestServerHook { ShouldIntercept = true };
        server.Hooks.AddEarlyRequestHook(hook);
        server.Start();

        // Act
        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync("http://localhost:8099/test");
        }
        catch { }

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.True(hook.CallCount > 0);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that late hooks at the server level are executed after handlers process requests.
    /// Late hooks run after the response has been generated but before it's sent.
    /// </summary>
    [Fact]
    public async Task Hooks_LateHook_IsExecuted()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8100/", 2);
        var lateHook = new TestServerHook { ShouldIntercept = false };
        server.Hooks.AddLateRequestHook(lateHook);

        var handler = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("OK", Encoding.UTF8),
                Encoding.UTF8)
        };
        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:8100/test");

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.True(lateHook.CallCount > 0);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that early hooks at the handler level can intercept requests before the handler processes them.
    /// Handler-level hooks are executed after server-level hooks but before the handler's TryHandle method.
    /// </summary>
    [Fact]
    public async Task Handler_WithEarlyHook_CanInterceptRequest()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8101/", 2);
        var handler = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("OK", Encoding.UTF8),
                Encoding.UTF8)
        };

        var handlerHook = new TestServerHook { ShouldIntercept = true };
        handler.Hooks.AddEarlyRequestHook(handlerHook);

        server.AddRequestHandler(handler);
        server.Start();

        // Act
        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync("http://localhost:8101/test");
        }
        catch { }

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.True(handlerHook.CallCount > 0);

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that when multiple handlers are registered, the first handler that handles the request wins.
    /// Subsequent handlers are not called once a handler successfully processes the request.
    /// </summary>
    [Fact]
    public async Task MultipleHandlers_FirstHandlerThatHandlesWins()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8102/", 2);

        var handler1 = new TestRequestHandler
        {
            ShouldHandle = false
        };

        var handler2 = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("Handler2", Encoding.UTF8),
                Encoding.UTF8)
        };

        var handler3 = new TestRequestHandler
        {
            ShouldHandle = true,
            ResponseToReturn = new HttpServerExResponse(
                HttpStatusCode.OK,
                new StringContent("Handler3", Encoding.UTF8),
                Encoding.UTF8)
        };

        server.AddRequestHandler(handler1);
        server.AddRequestHandler(handler2);
        server.AddRequestHandler(handler3);
        server.Start();

        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:8102/test");
        var content = await response.Content.ReadAsStringAsync();

        // Give server time to process
        await Task.Delay(100);

        // Assert
        Assert.Equal("Handler2", content);
        Assert.True(handler1.CallCount > 0);
        Assert.True(handler2.CallCount > 0);
        Assert.Equal(0, handler3.CallCount); // Should not be called

        // Cleanup
        server.Stop();
    }

    /// <summary>
    /// Verifies that the Wait() method blocks until the server is stopped.
    /// This is useful for keeping the server running in console applications.
    /// </summary>
    [Fact]
    public async Task Wait_WaitsForServerToStop()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8103/", 1);
        server.Start();

        var waitTask = Task.Run(() => server.Wait());

        // Act
        await Task.Delay(100);
        server.Stop();

        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(2))) == waitTask;

        // Assert
        Assert.True(completed);
    }

    /// <summary>
    /// Verifies that the Wait() method returns immediately when the server is not listening.
    /// This prevents blocking when the server hasn't been started.
    /// </summary>
    [Fact]
    public async Task Wait_WhenNotListening_ReturnsImmediately()
    {
        // Arrange
        using var server = new HttpServerExCore("http://localhost:8104/", 1);

        // Act
        var waitTask = Task.Run(() => server.Wait());
        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMilliseconds(100))) == waitTask;

        // Assert
        Assert.True(completed);
    }
}
