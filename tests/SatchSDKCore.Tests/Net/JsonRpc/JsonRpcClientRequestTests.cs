using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SSC;
using SSC.Net.JsonRpc;

namespace SSC.Tests.Net.JsonRpc;

/// <summary>
/// Tests for JsonRpcClientRequest class which represents a JSON-RPC 2.0 request
/// with method name, parameters, and request ID.
/// </summary>
public class JsonRpcClientRequestTests
{
    /// <summary>
    /// Verifies that the JsonRpcVersion property defaults to "2.0" as per JSON-RPC 2.0 specification.
    /// </summary>
    [Fact]
    public void JsonRpcVersion_DefaultsTo2Point0()
    {
        // Arrange & Act
        var request = new JsonRpcClientRequest
        {
            Method = "testMethod"
        };

        // Assert
        Assert.Equal("2.0", request.JsonRpcVersion);
    }

    /// <summary>
    /// Verifies that the Id property defaults to 1 when not explicitly set.
    /// </summary>
    [Fact]
    public void Id_DefaultsTo1()
    {
        // Arrange & Act
        var request = new JsonRpcClientRequest
        {
            Method = "testMethod"
        };

        // Assert
        Assert.Equal(1, request.Id);
    }

    /// <summary>
    /// Verifies that the Method property can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public void Method_CanBeSet()
    {
        // Arrange
        var methodName = "myCustomMethod";

        // Act
        var request = new JsonRpcClientRequest
        {
            Method = methodName
        };

        // Assert
        Assert.Equal(methodName, request.Method);
    }

    /// <summary>
    /// Verifies that the Params property can be set with a JsonElement and retrieved correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Params_CanBeSet_WithJsonElement()
    {
        // Arrange
        var paramsArray = new[] { "param1", "param2", "param3" };
        var jsonElement = JsonSerializer.SerializeToElement(paramsArray);

        // Act
        var request = new JsonRpcClientRequest
        {
            Method = "testMethod",
            Params = jsonElement
        };

        // Assert
        Assert.NotNull(request.Params);
        Assert.Equal(JsonValueKind.Array, request.Params.Value.ValueKind);
    }

    /// <summary>
    /// Verifies that the Params property can be null, which is valid for methods with no parameters.
    /// </summary>
    [Fact]
    public void Params_CanBeNull()
    {
        // Arrange & Act
        var request = new JsonRpcClientRequest
        {
            Method = "testMethod",
            Params = null
        };

        // Assert
        Assert.Null(request.Params);
    }

    /// <summary>
    /// Verifies that the Id property can be set to a custom value.
    /// </summary>
    [Fact]
    public void Id_CanBeSetToCustomValue()
    {
        // Arrange
        var customId = 42;

        // Act
        var request = new JsonRpcClientRequest
        {
            Method = "testMethod",
            Id = customId
        };

        // Assert
        Assert.Equal(customId, request.Id);
    }

    /// <summary>
    /// Verifies that a complete request serializes correctly to JSON with all properties.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_SerializesToJson_WithAllProperties()
    {
        // Arrange
        var parameters = new[] { "value1", "value2" };
        var request = new JsonRpcClientRequest
        {
            Method = "subtract",
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions),
            Id = 5
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcClientRequest>(json, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("2.0", deserialized.JsonRpcVersion);
        Assert.Equal("subtract", deserialized.Method);
        Assert.Equal(5, deserialized.Id);
        Assert.NotNull(deserialized.Params);
    }

    /// <summary>
    /// Verifies that a request serializes correctly to JSON with array parameters.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_SerializesToJson_WithArrayParams()
    {
        // Arrange
        var parameters = new object[] { 42, 23 };
        var request = new JsonRpcClientRequest
        {
            Method = "subtract",
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions),
            Id = 1
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.Contains("\"jsonrpc\":\"2.0\"", json);
        Assert.Contains("\"method\":\"subtract\"", json);
        Assert.Contains("\"id\":1", json);
        Assert.Contains("\"params\":", json);
    }

    /// <summary>
    /// Verifies that a request serializes correctly to JSON with object (named) parameters.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_SerializesToJson_WithObjectParams()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "subtrahend", 23 },
            { "minuend", 42 }
        };
        var request = new JsonRpcClientRequest
        {
            Method = "subtract",
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions),
            Id = 2
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.Contains("\"jsonrpc\":\"2.0\"", json);
        Assert.Contains("\"method\":\"subtract\"", json);
        Assert.Contains("\"id\":2", json);
        Assert.Contains("\"params\":", json);
    }

    /// <summary>
    /// Verifies that a request serializes correctly to JSON without parameters (Params is null).
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_SerializesToJson_WithoutParams()
    {
        // Arrange
        var request = new JsonRpcClientRequest
        {
            Method = "getServerInfo",
            Params = null,
            Id = 3
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.Contains("\"jsonrpc\":\"2.0\"", json);
        Assert.Contains("\"method\":\"getServerInfo\"", json);
        Assert.Contains("\"id\":3", json);
    }

    /// <summary>
    /// Verifies that JsonRpcVersion can be overridden to a custom value (though not recommended).
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void JsonRpcVersion_CanBeOverridden()
    {
        // Arrange & Act
        var request = new JsonRpcClientRequest
        {
            JsonRpcVersion = "1.0",
            Method = "testMethod"
        };

        // Assert
        Assert.Equal("1.0", request.JsonRpcVersion);
    }

    /// <summary>
    /// Verifies that the request handles complex nested parameters correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_HandlesComplexParameters()
    {
        // Arrange
        var complexParams = new
        {
            user = new { id = 123, name = "John" },
            options = new { timeout = 30, retries = 3 }
        };
        var request = new JsonRpcClientRequest
        {
            Method = "complexMethod",
            Params = JsonSerializer.SerializeToElement(complexParams, SDKConfig.JsonSerializerOptions),
            Id = 10
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcClientRequest>(json, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Params);
        Assert.Equal("complexMethod", deserialized.Method);
    }

    /// <summary>
    /// Verifies that the request handles empty array parameters correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_HandlesEmptyArrayParams()
    {
        // Arrange
        var emptyArray = Array.Empty<object>();
        var request = new JsonRpcClientRequest
        {
            Method = "methodWithEmptyParams",
            Params = JsonSerializer.SerializeToElement(emptyArray, SDKConfig.JsonSerializerOptions),
            Id = 15
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.Contains("\"params\":[]", json);
    }

    /// <summary>
    /// Verifies that the request handles empty object parameters correctly.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void Request_HandlesEmptyObjectParams()
    {
        // Arrange
        var emptyObject = new Dictionary<string, object>();
        var request = new JsonRpcClientRequest
        {
            Method = "methodWithEmptyObject",
            Params = JsonSerializer.SerializeToElement(emptyObject, SDKConfig.JsonSerializerOptions),
            Id = 16
        };

        // Act
        var json = JsonSerializer.Serialize(request, SDKConfig.JsonSerializerOptions);

        // Assert
        Assert.Contains("\"params\":{}", json);
    }

    /// <summary>
    /// Verifies that multiple requests with different IDs can be created independently.
    /// </summary>
    [Fact]
    public void MultipleRequests_CanHaveDifferentIds()
    {
        // Arrange & Act
        var request1 = new JsonRpcClientRequest { Method = "method1", Id = 1 };
        var request2 = new JsonRpcClientRequest { Method = "method2", Id = 2 };
        var request3 = new JsonRpcClientRequest { Method = "method3", Id = 3 };

        // Assert
        Assert.Equal(1, request1.Id);
        Assert.Equal(2, request2.Id);
        Assert.Equal(3, request3.Id);
        Assert.NotEqual(request1.Id, request2.Id);
        Assert.NotEqual(request2.Id, request3.Id);
    }
}
