using System.Net;
using System.Text;
using SSC.APIServer.Request;
using SSC.Net.HttpEx;

namespace SatchSDKCore.Tests.APIServer.Request;

/// <summary>
/// Tests for HTTPRequest class which wraps HttpServerExRequestContext to provide
/// request handling functionality including header access, IP address retrieval,
/// and body content reading.
/// </summary>
public class HTTPRequestTests
{
    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HTTPRequest(null!));
    }

    /// <summary>
    /// Verifies that constructor properly stores the context.
    /// </summary>
    [Fact]
    public async Task Constructor_WithValidContext_StoresContext()
    {
        // Arrange - Set up HTTP listener and wait for incoming request
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9989/");
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
            _ = await client.GetAsync("http://localhost:9989/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Assert - Verify context is properly stored
            Assert.NotNull(request.Context);
            Assert.Same(serverContext, request.Context);
        }
    }

    /// <summary>
    /// Verifies that GetOriginIPAddress returns the correct IP address from the context.
    /// For localhost connections, this should be a loopback address.
    /// </summary>
    [Fact]
    public async Task GetOriginIPAddress_ReturnsIPFromContext()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9988/");
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
            _ = await client.GetAsync("http://localhost:9988/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act
            var ipAddress = request.GetOriginIPAddress();

            // Assert - Should be a loopback address for localhost
            Assert.NotNull(ipAddress);
            Assert.True(IPAddress.IsLoopback(ipAddress));
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue returns true and correct value for existing header.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithExistingHeader_ReturnsTrueAndValue()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9987/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
        try
        {
            _ = await client.GetAsync("http://localhost:9987/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act
            var result = request.TryGetHeaderValue("X-Custom-Header", out var value);

            // Assert
            Assert.True(result);
            Assert.Equal("TestValue", value);
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue returns false for non-existing header.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithNonExistingHeader_ReturnsFalse()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9986/");
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
            _ = await client.GetAsync("http://localhost:9986/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act
            var result = request.TryGetHeaderValue("X-NonExistent-Header", out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue is case-insensitive for header names.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_IsCaseInsensitive()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9985/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Case-Test", "CaseValue");
        try
        {
            _ = await client.GetAsync("http://localhost:9985/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act - Try different case variations
            var result1 = request.TryGetHeaderValue("X-Case-Test", out var value1);
            var result2 = request.TryGetHeaderValue("x-case-test", out var value2);
            var result3 = request.TryGetHeaderValue("X-CASE-TEST", out var value3);

            // Assert - All should work due to case-insensitive nature of HTTP headers
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.Equal("CaseValue", value1);
            Assert.Equal("CaseValue", value2);
            Assert.Equal("CaseValue", value3);
        }
    }

    /// <summary>
    /// Verifies that multiple headers can be retrieved.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithMultipleHeaders_CanRetrieveAll()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9984/");
        listener.Start();

        HttpListenerContext? capturedContext = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();
            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Header-1", "Value1");
        client.DefaultRequestHeaders.Add("X-Header-2", "Value2");
        client.DefaultRequestHeaders.Add("X-Header-3", "Value3");
        try
        {
            _ = await client.GetAsync("http://localhost:9984/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act & Assert
            Assert.True(request.TryGetHeaderValue("X-Header-1", out var value1));
            Assert.Equal("Value1", value1);

            Assert.True(request.TryGetHeaderValue("X-Header-2", out var value2));
            Assert.Equal("Value2", value2);

            Assert.True(request.TryGetHeaderValue("X-Header-3", out var value3));
            Assert.Equal("Value3", value3);
        }
    }

    /// <summary>
    /// Verifies that GetBody reads the request body content correctly.
    /// </summary>
    [Fact]
    public async Task GetBody_ReadsRequestBodyContent()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9983/");
        listener.Start();

        const string expectedBody = "{\"test\":\"data\"}";
        HttpListenerContext? capturedContext = null;
        string? actualBody = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();

            // Read body immediately while stream is still available
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);
            actualBody = request.GetBody();

            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            var content = new StringContent(expectedBody, Encoding.UTF8, "application/json");
            _ = await client.PostAsync("http://localhost:9983/test", content);
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        Assert.NotNull(actualBody);
        Assert.Equal(expectedBody, actualBody);
    }

    /// <summary>
    /// Verifies that GetBody returns empty string for requests with no body.
    /// </summary>
    [Fact]
    public async Task GetBody_WithNoBody_ReturnsEmptyString()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9982/");
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
            _ = await client.GetAsync("http://localhost:9982/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act
            var body = request.GetBody();

            // Assert
            Assert.Equal(string.Empty, body);
        }
    }

    /// <summary>
    /// Verifies that AsHTTPRequest property from IRequest works correctly.
    /// </summary>
    [Fact]
    public async Task AsHTTPRequest_ReturnsItself()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9981/");
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
            _ = await client.GetAsync("http://localhost:9981/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);
            IRequest iRequest = request;

            // Act
            var asHTTPRequest = iRequest.AsHTTPRequest;

            // Assert
            Assert.NotNull(asHTTPRequest);
            Assert.Same(request, asHTTPRequest);
        }
    }

    /// <summary>
    /// Verifies that HTTPRequest properly inherits from IRequest.
    /// </summary>
    [Fact]
    public async Task HTTPRequest_InheritsFromIRequest()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9980/");
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
            _ = await client.GetAsync("http://localhost:9980/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act & Assert
            Assert.IsAssignableFrom<IRequest>(request);
        }
    }

    /// <summary>
    /// Verifies that GetBody handles UTF-8 encoded content correctly.
    /// </summary>
    [Fact]
    public async Task GetBody_WithUTF8Content_ReadsCorrectly()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9979/");
        listener.Start();

        const string expectedBody = "Hello 世界 🌍";
        HttpListenerContext? capturedContext = null;
        string? actualBody = null;
        var requestTask = Task.Run(async () =>
        {
            capturedContext = await listener.GetContextAsync();

            // Read body immediately while stream is still available
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);
            actualBody = request.GetBody();

            capturedContext.Response.StatusCode = 200;
            capturedContext.Response.Close();
        });

        using var client = new HttpClient();
        try
        {
            var content = new StringContent(expectedBody, Encoding.UTF8, "text/plain");
            _ = await client.PostAsync("http://localhost:9979/test", content);
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        Assert.NotNull(actualBody);
        Assert.Equal(expectedBody, actualBody);
    }

    /// <summary>
    /// Verifies that TryGetHeaderValue handles headers with multiple values correctly.
    /// </summary>
    [Fact]
    public async Task TryGetHeaderValue_WithMultipleValues_ReturnsCombined()
    {
        // Arrange
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:9978/");
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
            _ = await client.GetAsync("http://localhost:9978/test");
        }
        catch { }

        await Task.WhenAny(requestTask, Task.Delay(TimeSpan.FromSeconds(2)));

        if (capturedContext != null)
        {
            var serverContext = new HttpServerExRequestContext(capturedContext);
            var request = new HTTPRequest(serverContext);

            // Act
            var result = request.TryGetHeaderValue("X-Multi-Header", out var value);

            // Assert
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Contains("Value1", value);
            Assert.Contains("Value2", value);
        }
    }
}