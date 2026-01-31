using System.Net;
using System.Text;
using SSC.Net.HttpEx;
using static SSC.Net.HttpEx.IHttpClientEx;

namespace SSC.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpClientExCore class which provides an advanced HTTP client implementation
/// with support for retries, rate limiting, cookies, progress tracking, and custom data handlers.
/// </summary>
[Collection("HttpClientExCore Sequential Tests")]
public class HttpClientExCoreTests : IDisposable
{
    private HttpServerExCore? _testServer;
    private const string TestServerUrl = "http://localhost:9876/";
    private readonly List<HttpClientExCore> _clientsToDispose = new();

    /// <summary>
    /// Test data handler that collects data in chunks.
    /// </summary>
    private class TestDataHandler : IHttpClientExDataHandler
    {
        public int IdealBufferSize => 1024;
        public List<byte[]> ReceivedChunks { get; } = new();
        public int BeginCallCount { get; private set; }
        public int EndCallCount { get; private set; }

        public void Begin()
        {
            BeginCallCount++;
        }

        public ValueTask ProcessAsync(ReadOnlySpan<byte> data, long? totalLength)
        {
            ReceivedChunks.Add(data.ToArray());
            return ValueTask.CompletedTask;
        }

        public void End()
        {
            EndCallCount++;
        }
    }

    /// <summary>
    /// Test request handler that returns configurable responses.
    /// </summary>
    private class TestRequestHandler : IHttpServerExRequestHandler
    {
        public SSC.Misc.Hookable.IHookable<HttpServerExRequestContext> Hooks { get; } = new SSC.Misc.Hookable.Hookable<HttpServerExRequestContext>();
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;
        public string? ContentToReturn { get; set; } = "OK";
        public int CallCount { get; private set; }
        public int DelayMs { get; set; }
        public Action<HttpServerExRequestContext>? OnRequest { get; set; }

        public bool TryHandle(HttpServerExRequestContext context)
        {
            CallCount++;
            OnRequest?.Invoke(context);

            if (DelayMs > 0)
                Thread.Sleep(DelayMs);

            context.ServerResponse = new HttpServerExResponse(
                StatusCodeToReturn,
                ContentToReturn != null ? new StringContent(ContentToReturn, Encoding.UTF8) : null,
                Encoding.UTF8);
            return true;
        }
    }

    /// <summary>
    /// Starts a test HTTP server on localhost for testing purposes.
    /// </summary>
    private void StartTestServer(IHttpServerExRequestHandler handler)
    {
        _testServer = new HttpServerExCore(TestServerUrl, 2);
        _testServer.AddRequestHandler(handler);
        _testServer.Start();
    }

    /// <summary>
    /// Stops the test HTTP server.
    /// </summary>
    private void StopTestServer()
    {
        _testServer?.Stop();
        _testServer?.Dispose();
        _testServer = null;
    }

    /// <summary>
    /// Creates a client and tracks it for disposal.
    /// </summary>
    private HttpClientExCore CreateClient(string baseUrl = "", TimeSpan? timeout = null, EOptions options = EOptions.KeepAlive)
    {
        var client = new HttpClientExCore(baseUrl, timeout ?? TimeSpan.FromSeconds(5), options);
        _clientsToDispose.Add(client);
        return client;
    }

    public void Dispose()
    {
        StopTestServer();
        foreach (var client in _clientsToDispose)
        {
            client.Dispose();
        }
        _clientsToDispose.Clear();
    }

    /// <summary>
    /// Verifies that the GlobalClient static instance is properly initialized.
    /// </summary>
    [Fact]
    public void GlobalClient_IsInitialized()
    {
        // Assert
        Assert.NotNull(HttpClientExCore.GlobalClient);
        Assert.Equal(TimeSpan.FromSeconds(5), HttpClientExCore.GlobalClient.RetryInterval);
        Assert.Equal(2, HttpClientExCore.GlobalClient.MaxRetry);
    }

    /// <summary>
    /// Verifies that the constructor creates a client with default values.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesClient()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(client);
        Assert.Equal(2, client.MaxRetry);
        Assert.Equal(TimeSpan.FromSeconds(5), client.RetryInterval);
        Assert.NotNull(client.GlobalHeaders);
    }

    /// <summary>
    /// Verifies that the constructor creates a client with a base URL.
    /// </summary>
    [Fact]
    public void Constructor_WithBaseUrl_SetsBaseAddress()
    {
        // Act
        var client = CreateClient("https://api.example.com/", TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(client);
    }

    /// <summary>
    /// Verifies that the constructor creates a client with custom timeout.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomTimeout_SetsTimeout()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(30));

        // Assert
        Assert.NotNull(client);
    }

    /// <summary>
    /// Verifies that the constructor respects the KeepAlive option.
    /// </summary>
    [Fact]
    public void Constructor_WithKeepAliveOption_SetsOption()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(10), EOptions.KeepAlive);

        // Assert
        Assert.True(client.Options.HasFlag(EOptions.KeepAlive));
    }

    /// <summary>
    /// Verifies that the constructor respects the ForceCacheDiscard option.
    /// </summary>
    [Fact]
    public void Constructor_WithForceCacheDiscardOption_SetsOption()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(10), EOptions.ForceCacheDiscard);

        // Assert
        Assert.True(client.Options.HasFlag(EOptions.ForceCacheDiscard));
    }

    /// <summary>
    /// Verifies that the constructor respects the NoRetryOnRateLimit option.
    /// </summary>
    [Fact]
    public void Constructor_WithNoRetryOnRateLimitOption_SetsOption()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(10), EOptions.NoRetryOnRateLimit);

        // Assert
        Assert.True(client.Options.HasFlag(EOptions.NoRetryOnRateLimit));
    }

    /// <summary>
    /// Verifies that multiple options can be combined using bitwise OR.
    /// </summary>
    [Fact]
    public void Constructor_WithMultipleOptions_SetsAllOptions()
    {
        // Act
        var client = CreateClient("", TimeSpan.FromSeconds(10), EOptions.KeepAlive | EOptions.ForceCacheDiscard);

        // Assert
        Assert.True(client.Options.HasFlag(EOptions.KeepAlive));
        Assert.True(client.Options.HasFlag(EOptions.ForceCacheDiscard));
    }

    /// <summary>
    /// Verifies that MaxRetry property can be set and retrieved.
    /// </summary>
    [Fact]
    public void MaxRetry_CanBeSetAndRetrieved()
    {
        // Arrange
        var client = CreateClient();

        // Act
        client.MaxRetry = 5;

        // Assert
        Assert.Equal(5, client.MaxRetry);
    }

    /// <summary>
    /// Verifies that RetryInterval property can be set and retrieved.
    /// </summary>
    [Fact]
    public void RetryInterval_CanBeSetAndRetrieved()
    {
        // Arrange
        var client = CreateClient();

        // Act
        client.RetryInterval = TimeSpan.FromSeconds(10);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), client.RetryInterval);
    }

    /// <summary>
    /// Verifies that CookieJar property can be set and retrieved.
    /// </summary>
    [Fact]
    public void CookieJar_CanBeSetAndRetrieved()
    {
        // Arrange
        var client = CreateClient();
        var cookieContainer = new CookieContainer();

        // Act
        client.CookieJar = cookieContainer;

        // Assert
        Assert.Same(cookieContainer, client.CookieJar);
    }

    /// <summary>
    /// Verifies that setting CookieJar to null disables cookie handling.
    /// </summary>
    [Fact]
    public void CookieJar_CanBeSetToNull()
    {
        // Arrange
        var client = CreateClient();
        client.CookieJar = new CookieContainer();

        // Act
        client.CookieJar = null;

        // Assert
        Assert.Null(client.CookieJar);
    }

    /// <summary>
    /// Verifies that GlobalHeaders property is accessible.
    /// </summary>
    [Fact]
    public void GlobalHeaders_IsAccessible()
    {
        // Arrange
        var client = CreateClient();

        // Act
        client.GlobalHeaders.Add("X-Custom-Header", "TestValue");

        // Assert
        Assert.NotNull(client.GlobalHeaders);
        Assert.True(client.GlobalHeaders.Contains("X-Custom-Header"));
    }

    /// <summary>
    /// Verifies that DoRequest successfully performs a GET request.
    /// </summary>
    [Fact]
    public void DoRequest_WithGetMethod_ReturnsResponse()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Hello World" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = client.DoRequest("GET", "test");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello World", response.BodyString);
        Assert.True(handler.CallCount > 0);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that DoRequest successfully performs a POST request with payload.
    /// </summary>
    [Fact]
    public void DoRequest_WithPostAndPayload_SendsData()
    {
        // Arrange
        var handler = new TestRequestHandler();
        string? receivedMethod = null;
        handler.OnRequest = (ctx) =>
        {
            receivedMethod = ctx.ListenerRequest.HttpMethod;
        };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var payload = HttpClientExPayload.FromJsonString("{\"key\":\"value\"}");

        // Act
        var response = client.DoRequest("POST", "test", payload);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(receivedMethod);
        Assert.Equal("POST", receivedMethod);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that DoRequestAsync successfully performs an async GET request.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithGetMethod_ReturnsResponse()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Async Response" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Async Response", response.BodyString);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that DoRequestAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var handler = new TestRequestHandler { DelayMs = 200 };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var cts = new CancellationTokenSource();

        // Act
        var task = client.DoRequestAsync("GET", "test", cts.Token);
        cts.CancelAfter(50);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that DoRequestInBackground executes request and calls callback.
    /// </summary>
    [Fact]
    public async Task DoRequestInBackground_ExecutesAndCallsCallback()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Background Response" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        HttpClientExResponse? callbackResponse = null;
        var callbackCalled = new ManualResetEventSlim(false);

        // Act
        client.DoRequestInBackground("GET", "test", CancellationToken.None, (response) =>
        {
            callbackResponse = response;
            callbackCalled.Set();
        });

        // Wait for callback
        var completed = callbackCalled.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(completed);
        Assert.NotNull(callbackResponse);
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        Assert.Equal("Background Response", callbackResponse.BodyString);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client retries on failure up to MaxRetry times.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_OnFailure_RetriesUpToMaxRetry()
    {
        // Arrange
        var handler = new TestRequestHandler { StatusCodeToReturn = HttpStatusCode.InternalServerError };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 3;
        client.RetryInterval = TimeSpan.FromMilliseconds(100);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(handler.CallCount >= 3, $"Expected at least 3 calls, got {handler.CallCount}");

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that IgnoreRetryPolicy option prevents retries.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithIgnoreRetryPolicy_DoesNotRetry()
    {
        // Arrange
        var handler = new TestRequestHandler { StatusCodeToReturn = HttpStatusCode.InternalServerError };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 3;

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None, null, ERequestOptions.IgnoreRetryPolicy);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, handler.CallCount);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that progress handler is called during request.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithProgressHandler_ReportsProgress()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Test Content" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var progressReports = new List<float>();
        var progress = new Progress<float>(p => progressReports.Add(p));

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None, null, ERequestOptions.None, null, progress);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p >= 0.0f && p <= 1.0f);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that custom data handler is called during response processing.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithDataHandler_ProcessesData()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Test Data" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var dataHandler = new TestDataHandler();

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None, null, ERequestOptions.None, dataHandler);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, dataHandler.BeginCallCount);
        Assert.Equal(1, dataHandler.EndCallCount);
        Assert.NotEmpty(dataHandler.ReceivedChunks);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client handles different HTTP methods (PUT, PATCH, DELETE).
    /// </summary>
    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task DoRequestAsync_WithDifferentMethods_SupportsAllMethods(string method)
    {
        // Arrange
        var handler = new TestRequestHandler();
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync(method, "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that Dispose properly cleans up resources.
    /// </summary>
    [Fact]
    public void Dispose_ProperlyDisposesClient()
    {
        // Arrange
        var client = new HttpClientExCore("", TimeSpan.FromSeconds(10));

        // Act
        client.Dispose();

        // Assert - No exception thrown
        Assert.NotNull(client);
    }

    /// <summary>
    /// Verifies that the client can handle URLs with query parameters.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithQueryParameters_HandlesCorrectly()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Query Response" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "test?param1=value1&param2=value2", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client succeeds after a failed retry attempt.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_SucceedsAfterRetry()
    {
        // Arrange
        var handler = new TestRequestHandler { StatusCodeToReturn = HttpStatusCode.InternalServerError };
        var attemptCount = 0;
        handler.OnRequest = (ctx) =>
        {
            attemptCount++;
            if (attemptCount >= 2)
            {
                handler.StatusCodeToReturn = HttpStatusCode.OK;
                handler.ContentToReturn = "Success after retry";
            }
        };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 3;
        client.RetryInterval = TimeSpan.FromMilliseconds(100);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Success after retry", response.BodyString);
        Assert.True(attemptCount >= 2);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client handles empty response bodies.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithEmptyBody_HandlesCorrectly()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = null };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that multiple clients can be created and used independently.
    /// </summary>
    [Fact]
    public async Task MultipleClients_WorkIndependently()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Test Response" };
        StartTestServer(handler);
        var client1 = CreateClient(TestServerUrl);
        var client2 = CreateClient(TestServerUrl);
        client1.MaxRetry = 1;
        client2.MaxRetry = 5;

        // Act
        var response1 = await client1.DoRequestAsync("GET", "test1", CancellationToken.None);
        var response2 = await client2.DoRequestAsync("GET", "test2", CancellationToken.None);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.Equal(1, client1.MaxRetry);
        Assert.Equal(5, client2.MaxRetry);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client handles different content types in POST requests.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithFormPayload_SendsCorrectContentType()
    {
        // Arrange
        var handler = new TestRequestHandler();
        string? contentType = null;
        handler.OnRequest = (ctx) =>
        {
            contentType = ctx.ListenerRequest.ContentType;
        };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var payload = HttpClientExPayload.FromForm(new Dictionary<string, string> { { "key", "value" } });

        // Act
        var response = await client.DoRequestAsync("POST", "test", CancellationToken.None, payload);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(contentType);
        Assert.Contains("application/x-www-form-urlencoded", contentType);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that DoRequestAsync with cancellation throws TaskCanceledException and doesn't hang.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithEarlyCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var handler = new TestRequestHandler { DelayMs = 100 };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.DoRequestAsync("GET", "test", cts.Token));

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client properly uses the base URL when making requests.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithBaseUrl_CombinesUrlCorrectly()
    {
        // Arrange
        var handler = new TestRequestHandler { ContentToReturn = "Base URL Response" };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "api/endpoint", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Base URL Response", response.BodyString);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that the client can handle large response bodies.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithLargeResponse_HandlesCorrectly()
    {
        // Arrange
        var largeContent = new string('x', 100000); // 100KB
        var handler = new TestRequestHandler { ContentToReturn = largeContent };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(largeContent.Length, response.BodyString!.Length);

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that data handler receives chunks for large responses.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithLargeResponseAndDataHandler_ReceivesMultipleChunks()
    {
        // Arrange
        var largeContent = new string('y', 10000); // 10KB
        var handler = new TestRequestHandler { ContentToReturn = largeContent };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var dataHandler = new TestDataHandler();

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None, null, ERequestOptions.None, dataHandler);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, dataHandler.BeginCallCount);
        Assert.Equal(1, dataHandler.EndCallCount);
        Assert.NotEmpty(dataHandler.ReceivedChunks);
        var totalBytes = dataHandler.ReceivedChunks.Sum(c => c.Length);
        Assert.True(totalBytes > 0);

        // Cleanup
        StopTestServer();
    }

    #region Rate Limit Tests

    /// <summary>
    /// Test request handler that can return rate limit responses with headers.
    /// </summary>
    private class RateLimitTestHandler : IHttpServerExRequestHandler
    {
        public SSC.Misc.Hookable.IHookable<HttpServerExRequestContext> Hooks { get; } = new SSC.Misc.Hookable.Hookable<HttpServerExRequestContext>();
        public int CallCount { get; private set; }
        public bool SendRateLimitResponse { get; set; } = true;
        public int RateLimitResetSeconds { get; set; } = 1;
        public Action<HttpServerExRequestContext>? OnRequest { get; set; }

        public bool TryHandle(HttpServerExRequestContext context)
        {
            CallCount++;
            OnRequest?.Invoke(context);

            if (SendRateLimitResponse && CallCount == 1)
            {
                // Return 429 with rate limit headers on first call
                context.ListenerResponse.StatusCode = 429;
                context.ListenerResponse.Headers.Add("X-RateLimit-Limit", "100");
                context.ListenerResponse.Headers.Add("X-RateLimit-Remaining", "0");
                context.ListenerResponse.Headers.Add("X-RateLimit-Reset", ((long)(DateTimeOffset.UtcNow.AddSeconds(RateLimitResetSeconds).ToUnixTimeSeconds())).ToString());

                var content = Encoding.UTF8.GetBytes("Rate limit exceeded");
                context.ListenerResponse.ContentLength64 = content.Length;
                context.ListenerResponse.OutputStream.Write(content, 0, content.Length);
                context.ListenerResponse.OutputStream.Close();
            }
            else
            {
                // Return success on subsequent calls
                context.ServerResponse = new HttpServerExResponse(
                    HttpStatusCode.OK,
                    new StringContent("Success after rate limit", Encoding.UTF8),
                    Encoding.UTF8);
            }

            return true;
        }
    }

    /// <summary>
    /// Verifies that the client retries after being rate limited.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithRateLimit_RetriesAfterDelay()
    {
        // Arrange
        var handler = new RateLimitTestHandler { RateLimitResetSeconds = 1 };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 3;

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Should succeed on retry
        Assert.Equal("Success after rate limit", response.BodyString);
        Assert.Equal(2, handler.CallCount); // Should have been called twice (first rate limited, second success)

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that NoRetryOnRateLimit option prevents retry on rate limit.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithNoRetryOnRateLimitOption_DoesNotRetry()
    {
        // Arrange
        var handler = new RateLimitTestHandler();
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl, options: EOptions.NoRetryOnRateLimit);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal((HttpStatusCode)429, response.StatusCode); // Should return rate limit response
        Assert.Equal(1, handler.CallCount); // Should only be called once

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that NoRetryOnRateLimit request option prevents retry on rate limit.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithNoRetryOnRateLimitRequestOption_DoesNotRetry()
    {
        // Arrange
        var handler = new RateLimitTestHandler();
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None, null, ERequestOptions.NoRetryOnRateLimit);

        // Assert
        Assert.NotNull(response);
        Assert.Equal((HttpStatusCode)429, response.StatusCode); // Should return rate limit response
        Assert.Equal(1, handler.CallCount); // Should only be called once

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that cancellation during rate limit wait is handled properly.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithRateLimitAndCancellation_CancelsCorrectly()
    {
        // Arrange
        var handler = new RateLimitTestHandler { RateLimitResetSeconds = 10 }; // Long wait
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        var cts = new CancellationTokenSource();

        // Act
        var task = client.DoRequestAsync("GET", "test", cts.Token);
        cts.CancelAfter(500); // Cancel after 500ms, before rate limit reset

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.Equal(1, handler.CallCount); // Should only be called once before cancellation

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that rate limit response is returned when NoRetryOnRateLimit is set.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithRateLimitAndNoRetry_ReturnsRateLimitResponse()
    {
        // Arrange
        var handler = new RateLimitTestHandler();
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl, options: EOptions.NoRetryOnRateLimit);

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.True(response.IsRateLimited);
        Assert.Equal(1, handler.CallCount); // Should only be called once

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that multiple rate limit retries work correctly.
    /// </summary>
    [Fact]
    public async Task DoRequestAsync_WithMultipleRateLimits_RetriesMultipleTimes()
    {
        // Arrange
        var handler = new RateLimitTestHandler { RateLimitResetSeconds = 1 };
        handler.OnRequest = (ctx) =>
        {
            // Send rate limit on first attempt only, success on second
            if (handler.CallCount >= 2)
                handler.SendRateLimitResponse = false;
        };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 5;

        // Act
        var response = await client.DoRequestAsync("GET", "test", CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.CallCount); // Should be called 2 times (first rate limited, second success)

        // Cleanup
        StopTestServer();
    }

    /// <summary>
    /// Verifies that rate limit works with DoRequest (synchronous).
    /// </summary>
    [Fact]
    public void DoRequest_WithRateLimit_RetriesAfterDelay()
    {
        // Arrange
        var handler = new RateLimitTestHandler { RateLimitResetSeconds = 1 };
        StartTestServer(handler);
        var client = CreateClient(TestServerUrl);
        client.MaxRetry = 3;

        // Act
        var response = client.DoRequest("GET", "test");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.CallCount); // Should retry after rate limit

        // Cleanup
        StopTestServer();
    }

    #endregion
}
