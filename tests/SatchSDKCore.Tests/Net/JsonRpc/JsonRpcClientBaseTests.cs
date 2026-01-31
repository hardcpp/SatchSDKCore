using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SSC.Net.JsonRpc;
using SSC;
using static SSC.Net.JsonRpc.IJsonRpcClient;

namespace SSC.Tests.Net.JsonRpc;

/// <summary>
/// Tests for JsonRpcClientBase abstract class which provides the base implementation
/// for JSON-RPC 2.0 client functionality with parameter serialization.
/// </summary>
public class JsonRpcClientBaseTests
{
    /// <summary>
    /// Test implementation of JsonRpcClientBase for testing purposes.
    /// </summary>
    private class TestJsonRpcClient : JsonRpcClientBase
    {
        public JsonRpcClientRequest? LastRequest { get; private set; }
        public ECallOptions LastOptions { get; private set; }
        public JsonRpcClientResult? ResultToReturn { get; set; }
        public bool CallInBackgroundWasCalled { get; private set; }
        public bool CallAsyncWasCalled { get; private set; }

        [RequiresUnreferencedCode("Test")]
        [RequiresDynamicCode("Test")]
        protected override JsonRpcClientResult? DoCall(JsonRpcClientRequest request, ECallOptions options)
        {
            LastRequest = request;
            LastOptions = options;
            return ResultToReturn;
        }

        [RequiresUnreferencedCode("Test")]
        [RequiresDynamicCode("Test")]
        protected override void DoCallInBackground(
            JsonRpcClientRequest request,
            CancellationToken cancellationToken,
            Action<JsonRpcClientResult?>? callback,
            ECallOptions options)
        {
            LastRequest = request;
            LastOptions = options;
            CallInBackgroundWasCalled = true;
            callback?.Invoke(ResultToReturn);
        }

        [RequiresUnreferencedCode("Test")]
        [RequiresDynamicCode("Test")]
        protected override Task<JsonRpcClientResult?> DoCallAsync(
            JsonRpcClientRequest request,
            CancellationToken cancellationToken,
            ECallOptions options)
        {
            LastRequest = request;
            LastOptions = options;
            CallAsyncWasCalled = true;
            return Task.FromResult(ResultToReturn);
        }
    }

    /// <summary>
    /// Verifies that Call with array parameters creates a request with serialized array params.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithArrayParameters_CreatesCorrectRequest()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "value1", 42, true };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Array, client.LastRequest.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that Call with dictionary parameters creates a request with serialized object params.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithDictionaryParameters_CreatesCorrectRequest()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new Dictionary<string, object>
        {
            { "param1", "value1" },
            { "param2", 42 }
        };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Object, client.LastRequest.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that Call with default options sets ECallOptions.None.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithDefaultOptions_SetsNoneOptions()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.Equal(ECallOptions.None, client.LastOptions);
    }

    /// <summary>
    /// Verifies that Call with IgnoreRetryPolicy option passes it correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithIgnoreRetryPolicyOption_PassesOptionCorrectly()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };

        // Act
        client.Call("testMethod", parameters, ECallOptions.IgnoreRetryPolicy);

        // Assert
        Assert.Equal(ECallOptions.IgnoreRetryPolicy, client.LastOptions);
    }

    /// <summary>
    /// Verifies that Call returns the result from DoCall.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_ReturnsResultFromDoCall()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var expectedResult = new JsonRpcClientResult();
        client.ResultToReturn = expectedResult;
        var parameters = new object[] { "test" };

        // Act
        var result = client.Call("testMethod", parameters);

        // Assert
        Assert.Same(expectedResult, result);
    }

    /// <summary>
    /// Verifies that Call with empty array parameters works correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithEmptyArrayParameters_CreatesCorrectRequest()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = Array.Empty<object>();

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Array, client.LastRequest.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that Call with empty dictionary parameters works correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithEmptyDictionaryParameters_CreatesCorrectRequest()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new Dictionary<string, object>();

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Object, client.LastRequest.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that CallInBackground with array parameters invokes DoCallInBackground.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void CallInBackground_WithArrayParameters_InvokesDoCallInBackground()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };
        var cancellationToken = CancellationToken.None;
        var callbackInvoked = false;
        Action<JsonRpcClientResult?>? callback = (result) => { callbackInvoked = true; };

        // Act
        client.CallInBackground("testMethod", parameters, cancellationToken, callback);

        // Assert
        Assert.True(client.CallInBackgroundWasCalled);
        Assert.True(callbackInvoked);
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
    }

    /// <summary>
    /// Verifies that CallInBackground with dictionary parameters invokes DoCallInBackground.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void CallInBackground_WithDictionaryParameters_InvokesDoCallInBackground()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new Dictionary<string, object> { { "key", "value" } };
        var cancellationToken = CancellationToken.None;
        var callbackInvoked = false;
        Action<JsonRpcClientResult?>? callback = (result) => { callbackInvoked = true; };

        // Act
        client.CallInBackground("testMethod", parameters, cancellationToken, callback);

        // Assert
        Assert.True(client.CallInBackgroundWasCalled);
        Assert.True(callbackInvoked);
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
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
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = Record.Exception(() =>
            client.CallInBackground("testMethod", parameters, cancellationToken, null));

        Assert.Null(exception);
        Assert.True(client.CallInBackgroundWasCalled);
    }

    /// <summary>
    /// Verifies that CallAsync with array parameters invokes DoCallAsync and returns result.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_WithArrayParameters_InvokesDoCallAsync()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var expectedResult = new JsonRpcClientResult();
        client.ResultToReturn = expectedResult;
        var parameters = new object[] { "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await client.CallAsync("testMethod", parameters, cancellationToken);

        // Assert
        Assert.True(client.CallAsyncWasCalled);
        Assert.Same(expectedResult, result);
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
    }

    /// <summary>
    /// Verifies that CallAsync with dictionary parameters invokes DoCallAsync and returns result.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_WithDictionaryParameters_InvokesDoCallAsync()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var expectedResult = new JsonRpcClientResult();
        client.ResultToReturn = expectedResult;
        var parameters = new Dictionary<string, object> { { "key", "value" } };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await client.CallAsync("testMethod", parameters, cancellationToken);

        // Assert
        Assert.True(client.CallAsyncWasCalled);
        Assert.Same(expectedResult, result);
        Assert.NotNull(client.LastRequest);
        Assert.Equal("testMethod", client.LastRequest.Method);
    }

    /// <summary>
    /// Verifies that CallAsync with default options sets ECallOptions.None.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_WithDefaultOptions_SetsNoneOptions()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        await client.CallAsync("testMethod", parameters, cancellationToken);

        // Assert
        Assert.Equal(ECallOptions.None, client.LastOptions);
    }

    /// <summary>
    /// Verifies that CallAsync with IgnoreRetryPolicy option passes it correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public async Task CallAsync_WithIgnoreRetryPolicyOption_PassesOptionCorrectly()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[] { "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        await client.CallAsync("testMethod", parameters, cancellationToken, ECallOptions.IgnoreRetryPolicy);

        // Assert
        Assert.Equal(ECallOptions.IgnoreRetryPolicy, client.LastOptions);
    }

    /// <summary>
    /// Verifies that Call with complex object parameters serializes correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithComplexObjectParameters_SerializesCorrectly()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new object[]
        {
            new { Id = 1, Name = "Test" },
            new[] { 1, 2, 3 }
        };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Array, client.LastRequest.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that Call with nested dictionary parameters serializes correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Call_WithNestedDictionaryParameters_SerializesCorrectly()
    {
        // Arrange
        var client = new TestJsonRpcClient();
        var parameters = new Dictionary<string, object>
        {
            { "outer", new Dictionary<string, object> { { "inner", "value" } } }
        };

        // Act
        client.Call("testMethod", parameters);

        // Assert
        Assert.NotNull(client.LastRequest);
        Assert.NotNull(client.LastRequest.Params);
        Assert.Equal(JsonValueKind.Object, client.LastRequest.Params.Value.ValueKind);
    }
}