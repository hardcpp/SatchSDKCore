using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SSC;
using SSC.Net.HttpEx;
using SSC.Net.JsonRpc;
using static SSC.Net.JsonRpc.IJsonRpcClient;

namespace SSC.Tests.Net.JsonRpc;

/// <summary>
/// Tests for JsonRpcClientHttp class which provides HTTP-based JSON-RPC 2.0 client implementation
/// using IHttpClientEx for making HTTP requests and handling responses.
/// </summary>
public class JsonRpcClientHttpTests
{
    /// <summary>
    /// Mock implementation of IHttpClientEx for testing purposes.
    /// </summary>
    private class MockHttpClientEx : IHttpClientEx
    {
        public HttpClientExResponse? ResponseToReturn { get; set; }
        public string? LastMethod { get; private set; }
        public string? LastUrl { get; private set; }
        public HttpClientExPayload? LastPayload { get; private set; }
        public IHttpClientEx.ERequestOptions LastOptions { get; private set; }

        public IHttpClientEx.EOptions Options => IHttpClientEx.EOptions.None;
        public int MaxRetry { get; set; } = 3;
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);
        public HttpRequestHeaders GlobalHeaders => new HttpClient().DefaultRequestHeaders;
        public CookieContainer? CookieJar { get; set; }

        public HttpClientExResponse DoRequest(
            string method,
            string url,
            HttpClientExPayload? payload = null,
            IHttpClientEx.ERequestOptions options = IHttpClientEx.ERequestOptions.None,
            IHttpClientExDataHandler? dataHandler = null,
            IProgress<float>? progressHandler = null)
        {
            LastMethod = method;
            LastUrl = url;
            LastPayload = payload;
            LastOptions = options;
            return ResponseToReturn ?? new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        }

        public void DoRequestInBackground(
            string method,
            string url,
            CancellationToken cancellationToken,
            Action<HttpClientExResponse?>? callback,
            HttpClientExPayload? payload = null,
            IHttpClientEx.ERequestOptions options = IHttpClientEx.ERequestOptions.None,
            IHttpClientExDataHandler? dataHandler = null,
            IProgress<float>? progressHandler = null)
        {
            LastMethod = method;
            LastUrl = url;
            LastPayload = payload;
            LastOptions = options;
            callback?.Invoke(ResponseToReturn);
        }

        public Task<HttpClientExResponse> DoRequestAsync(
            string method,
            string url,
            CancellationToken cancellationToken,
            HttpClientExPayload? payload = null,
            IHttpClientEx.ERequestOptions options = IHttpClientEx.ERequestOptions.None,
            IHttpClientExDataHandler? dataHandler = null,
            IProgress<float>? progressHandler = null)
        {
            LastMethod = method;
            LastUrl = url;
            LastPayload = payload;
            LastOptions = options;
            return Task.FromResult(ResponseToReturn ?? new HttpClientExResponse(HttpStatusCode.OK, "OK", true));
        }

        public void Dispose()
        {
            // Nothing to dispose in mock
        }
    }

    /// <summary>
    /// Verifies that the constructor initializes HttpClient property correctly.
    /// </summary>
    [Fact]
    public void Constructor_InitializesHttpClientProperty()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();

        // Act
        var client = new JsonRpcClientHttp(mockHttpClient);

        // Assert
        Assert.Same(mockHttpClient, client.HttpClient);
    }

    /// <summary>
    /// Verifies that the constructor initializes OverrideUrl property to null when not provided.
    /// </summary>
    [Fact]
    public void Constructor_WithoutOverrideUrl_SetsOverrideUrlToNull()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();

        // Act
        var client = new JsonRpcClientHttp(mockHttpClient);

        // Assert
        Assert.Null(client.OverrideUrl);
    }

    /// <summary>
    /// Verifies that the constructor initializes OverrideUrl property when provided.
    /// </summary>
    [Fact]
    public void Constructor_WithOverrideUrl_SetsOverrideUrlCorrectly()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var overrideUrl = "https://custom.api.com/rpc";

        // Act
        var client = new JsonRpcClientHttp(mockHttpClient, overrideUrl);

        // Assert
        Assert.Equal(overrideUrl, client.OverrideUrl);
    }

    /// <summary>
    /// Verifies that DoCall makes a POST request to the HTTP client.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_MakesPostRequest()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.Equal("POST", mockHttpClient.LastMethod);
    }

    /// <summary>
    /// Verifies that DoCall uses empty string URL when no override is set.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithoutOverrideUrl_UsesEmptyString()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.Equal(string.Empty, mockHttpClient.LastUrl);
    }

    /// <summary>
    /// Verifies that DoCall uses override URL when provided.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithOverrideUrl_UsesOverrideUrl()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var overrideUrl = "https://custom.api.com/rpc";
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient, overrideUrl);
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.Equal(overrideUrl, mockHttpClient.LastUrl);
    }

    /// <summary>
    /// Verifies that DoCall sends serialized JSON-RPC request as payload.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_SendsSerializedJsonRpcRequest()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "param1", 42 };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(mockHttpClient.LastPayload);
        var payloadString = Encoding.UTF8.GetString(mockHttpClient.LastPayload.Bytes);
        Assert.Contains("\"method\":\"testMethod\"", payloadString);
        Assert.Contains("\"jsonrpc\":\"2.0\"", payloadString);
    }

    /// <summary>
    /// Verifies that DoCall correctly parses successful JSON-RPC response.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ParsesSuccessfulResponse()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":{\"status\":\"ok\"},\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Verifies that DoCall correctly parses error JSON-RPC response.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ParsesErrorResponse()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32600,\"message\":\"Invalid Request\"},\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Result);
        Assert.NotNull(result.Error);
    }

    /// <summary>
    /// Verifies that DoCall returns null when HTTP response is null.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ReturnsNull_WhenHttpResponseIsNull()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        mockHttpClient.ResponseToReturn = null;
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DoCall returns null when HTTP response body is empty.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ReturnsNull_WhenResponseBodyIsEmpty()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        mockHttpClient.ResponseToReturn = CreateHttpResponse(string.Empty);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DoCall returns null when response contains invalid JSON.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ReturnsNull_WhenResponseIsInvalidJson()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        mockHttpClient.ResponseToReturn = CreateHttpResponse("Invalid JSON");
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DoCall with IgnoreRetryPolicy option maps to HTTP client option.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithIgnoreRetryPolicy_MapsToHttpClientOption()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters, ECallOptions.IgnoreRetryPolicy);

        // Assert
        Assert.True(mockHttpClient.LastOptions.HasFlag(IHttpClientEx.ERequestOptions.IgnoreRetryPolicy));
    }

    /// <summary>
    /// Verifies that DoCall with None option maps to HTTP client None option.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithNoneOption_MapsToHttpClientNoneOption()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters, ECallOptions.None);

        // Assert
        Assert.Equal(IHttpClientEx.ERequestOptions.None, mockHttpClient.LastOptions);
    }

    /// <summary>
    /// Verifies that CallInBackground invokes callback with parsed result.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void CallInBackground_InvokesCallbackWithParsedResult()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":{\"data\":\"test\"},\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };
        JsonRpcClientResult? callbackResult = null;
        Action<JsonRpcClientResult?> callback = (result) => { callbackResult = result; };

        // Act
        client.CallInBackground("testMethod", parameters, CancellationToken.None, callback);

        // Assert
        Assert.NotNull(callbackResult);
        Assert.NotNull(callbackResult.Result);
    }

    /// <summary>
    /// Verifies that CallInBackground works with null callback.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void CallInBackground_WithNullCallback_DoesNotThrow()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act & Assert
        var exception = Record.Exception(() =>
            client.CallInBackground("testMethod", parameters, CancellationToken.None, null));

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that CallAsync returns parsed result asynchronously.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_ReturnsParsedResultAsynchronously()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":{\"value\":123},\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        var result = await client.CallAsync("testMethod", parameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    /// <summary>
    /// Verifies that CallAsync uses POST method.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_UsesPostMethod()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new object[] { "test" };

        // Act
        await client.CallAsync("testMethod", parameters, CancellationToken.None);

        // Assert
        Assert.Equal("POST", mockHttpClient.LastMethod);
    }

    /// <summary>
    /// Verifies that Call with dictionary parameters works correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithDictionaryParameters_SendsCorrectPayload()
    {
        // Arrange
        var mockHttpClient = new MockHttpClientEx();
        var responseJson = "{\"jsonrpc\":\"2.0\",\"result\":\"success\",\"id\":1}";
        mockHttpClient.ResponseToReturn = CreateHttpResponse(responseJson);
        var client = new JsonRpcClientHttp(mockHttpClient);
        var parameters = new Dictionary<string, object>
        {
            { "param1", "value1" },
            { "param2", 42 }
        };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(mockHttpClient.LastPayload);
        var payloadString = Encoding.UTF8.GetString(mockHttpClient.LastPayload.Bytes);
        Assert.Contains("\"method\":\"testMethod\"", payloadString);
        Assert.Contains("\"params\":", payloadString);
    }

    /// <summary>
    /// Helper method to create an HttpClientExResponse with the given JSON body.
    /// </summary>
    private static HttpClientExResponse CreateHttpResponse(string jsonBody)
    {
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes(jsonBody));
        return response;
    }
}
