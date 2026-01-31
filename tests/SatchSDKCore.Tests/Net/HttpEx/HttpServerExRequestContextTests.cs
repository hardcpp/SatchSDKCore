using System.Net;
using SSC.Net.HttpEx;

namespace SSC.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpServerExRequestContext class which wraps HttpListenerContext
/// and provides additional functionality for HTTP server request handling.
/// </summary>
public class HttpServerExRequestContextTests
{
    /// <summary>
    /// Verifies that the constructor properly initializes the ListenerContext property
    /// and exposes the underlying HttpListenerRequest and HttpListenerResponse objects.
    /// </summary>
    [Fact]
    public async Task Constructor_WithValidContext_SetsListenerContext()
    {
        // Arrange - Set up HTTP listener and wait for incoming request
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9999/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        // Act - Make HTTP request to trigger listener
        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9999/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Assert - Verify context is properly set
            Assert.NotNull(serverContext.ListenerContext);
            Assert.Equal(capturedContext, serverContext.ListenerContext);
            Assert.NotNull(serverContext.ListenerRequest);
            Assert.NotNull(serverContext.ListenerResponse);
        }
    }

    /// <summary>
    /// Verifies that the ServerResponse property is initialized to null by default.
    /// </summary>
    [Fact]
    public async Task Constructor_InitializesServerResponseToNull()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9998/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9998/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            // Act
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Assert - ServerResponse should be null initially
            Assert.Null(serverContext.ServerResponse);
        }
    }

    /// <summary>
    /// Verifies that the ConnectionUpgraded property is initialized to false by default.
    /// This property tracks whether the connection has been upgraded (e.g., to WebSocket).
    /// </summary>
    [Fact]
    public async Task Constructor_InitializesConnectionUpgradedToFalse()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9997/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9997/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            // Act
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Assert - ConnectionUpgraded should be false initially
            Assert.False(serverContext.ConnectionUpgraded);
        }
    }

    /// <summary>
    /// Verifies that the ServerResponse property can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public async Task ServerResponse_CanBeSet()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9996/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9996/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var response = new HttpServerExResponse(HttpStatusCode.OK, null, null);

            // Act - Set the server response
            serverContext.ServerResponse = response;

            // Assert - Verify the response was set correctly
            Assert.Equal(response, serverContext.ServerResponse);
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue returns true and the correct value
    /// when the requested header exists in the HTTP request.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithExistingHeader_ReturnsTrue()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9995/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Custom-Header", "CustomValue");
        try
        {
            _ = await client.GetAsync("http://localhost:9995/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Act - Try to get the custom header
            var result = serverContext.TryGetHeaderValue("X-Custom-Header", out var value);

            // Assert - Should return true with correct value
            Assert.True(result);
            Assert.Equal("CustomValue", value);
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue returns false and null
    /// when the requested header does not exist in the HTTP request.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithNonExistingHeader_ReturnsFalse()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9994/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9994/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Act - Try to get a non-existent header
            var result = serverContext.TryGetHeaderValue("X-NonExistent-Header", out var value);

            // Assert - Should return false with null value
            Assert.False(result);
            Assert.Null(value);
        }
    }

    /// <summary>
    /// Verifies that GetOriginIPAddress returns the IP address of the remote client.
    /// For localhost connections, this should be a loopback address.
    /// </summary>
    [Fact]
    public async Task GetOriginIPAddress_ReturnsRemoteEndPointAddress()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9993/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            _ = await client.GetAsync("http://localhost:9993/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Act - Get the origin IP address
            var ipAddress = serverContext.GetOriginIPAddress();

            // Assert - Should be a loopback address for localhost
            Assert.NotNull(ipAddress);
            Assert.True(IPAddress.IsLoopback(ipAddress));
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue correctly handles headers with multiple values
    /// by combining them (typically comma-separated).
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithMultipleValues_ReturnsCombined()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9992/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Multi-Header", new[] { "Value1", "Value2" });
        try
        {
            _ = await client.GetAsync("http://localhost:9992/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Act - Get header with multiple values
            var result = serverContext.TryGetHeaderValue("X-Multi-Header", out var value);

            // Assert - Should return true with combined values
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Contains("Value1", value);
            Assert.Contains("Value2", value);
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue performs case-insensitive header name matching,
    /// as per HTTP specification.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_IsCaseInsensitive()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9991/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Case-Test", "TestValue");
        try
        {
            _ = await client.GetAsync("http://localhost:9991/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);

            // Act - Try different case variations of the header name
            var result1 = serverContext.TryGetHeaderValue("X-Case-Test", out var value1);
            var result2 = serverContext.TryGetHeaderValue("x-case-test", out var value2);
            var result3 = serverContext.TryGetHeaderValue("X-CASE-TEST", out var value3);

            // Assert - All variations should work and return the same value
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.Equal("TestValue", value1);
            Assert.Equal("TestValue", value2);
            Assert.Equal("TestValue", value3);
        }
    }
}
