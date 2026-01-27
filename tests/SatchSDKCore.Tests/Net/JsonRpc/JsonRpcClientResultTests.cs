using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using SSC.Net.JsonRpc;

namespace SatchSDKCore.Tests.Net.JsonRpc;

/// <summary>
/// Tests for JsonRpcClientResult class which encapsulates JSON-RPC 2.0 response data
/// containing either a result or error object, with support for typed deserialization.
/// </summary>
public class JsonRpcClientResultTests
{
    /// <summary>
    /// Verifies that the default constructor initializes both Result and Error properties to null.
    /// </summary>
    [Fact]
    public void Constructor_InitializesWithNullValues()
    {
        // Act
        var result = new JsonRpcClientResult();

        // Assert
        Assert.Null(result.Result);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Verifies that the Result property can be set with a JsonObject
    /// and that the data can be retrieved correctly.
    /// </summary>
    [Fact]
    public void Result_CanBeSet()
    {
        // Arrange
        var result = new JsonRpcClientResult();
        var jsonResult = new JsonObject
        {
            ["status"] = "success",
            ["data"] = "test data"
        };

        // Act
        result.Result = jsonResult;

        // Assert
        Assert.NotNull(result.Result);
        Assert.Equal("success", result.Result["status"]?.GetValue<string>());
        Assert.Equal("test data", result.Result["data"]?.GetValue<string>());
    }

    /// <summary>
    /// Verifies that the Error property can be set with a JsonObject containing
    /// JSON-RPC error information (code and message).
    /// </summary>
    [Fact]
    public void Error_CanBeSet()
    {
        // Arrange
        var result = new JsonRpcClientResult();
        var jsonError = new JsonObject
        {
            ["code"] = -32600,
            ["message"] = "Invalid Request"
        };

        // Act
        result.Error = jsonError;

        // Assert
        Assert.NotNull(result.Error);
        Assert.Equal(-32600, result.Error["code"]?.GetValue<int>());
        Assert.Equal("Invalid Request", result.Error["message"]?.GetValue<string>());
    }

    /// <summary>
    /// Verifies that TryGet() successfully deserializes a valid JSON result
    /// into a strongly-typed object.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_ReturnsTrue_WithValidResult()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse("{\"Name\":\"TestUser\",\"Age\":25}")!.AsObject()
        };

        // Act
        var success = result.TryGet<TestUser>(out var user);

        // Assert
        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal("TestUser", user.Name);
        Assert.Equal(25, user.Age);
    }

    /// <summary>
    /// Verifies that TryGet() returns false when the Result property is null,
    /// and sets the output parameter to null.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_ReturnsFalse_WithNullResult()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = null
        };

        // Act
        var success = result.TryGet<TestUser>(out var user);

        // Assert
        Assert.False(success);
        Assert.Null(user);
    }

    /// <summary>
    /// Verifies that TryGet() handles JSON with properties that don't match the target type
    /// by creating an object with default property values.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesPartialMatch()
    {
        // Arrange - JSON with properties that don't match the target type
        var result = new JsonRpcClientResult
        {
            Result = new JsonObject
            {
                ["InvalidProperty"] = "value"
            }
        };

        // Act
        var success = result.TryGet<TestUser>(out var user);

        // Assert - Deserialization succeeds but creates object with default values
        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(0, user.Age);
    }

    /// <summary>
    /// Verifies that TryGet() correctly deserializes complex nested objects
    /// with multiple levels of properties.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesComplexObjects()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse("{\"Name\":\"Complex\",\"Age\":30,\"Address\":{\"Street\":\"Main St\",\"City\":\"TestCity\"}}")!.AsObject()
        };

        // Act
        var success = result.TryGet<ComplexUser>(out var user);

        // Assert
        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal("Complex", user.Name);
        Assert.Equal(30, user.Age);
        Assert.NotNull(user.Address);
        Assert.Equal("Main St", user.Address.Street);
        Assert.Equal("TestCity", user.Address.City);
    }

    /// <summary>
    /// Verifies that TryGet() correctly deserializes JSON arrays into typed array properties.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesArrays()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse("{\"Items\":[1,2,3,4,5]}")!.AsObject()
        };

        // Act
        var success = result.TryGet<ItemCollection>(out var collection);

        // Assert
        Assert.True(success);
        Assert.NotNull(collection);
        Assert.NotNull(collection.Items);
        Assert.Equal(5, collection.Items.Length);
        Assert.Equal(1, collection.Items[0]);
        Assert.Equal(5, collection.Items[4]);
    }

    /// <summary>
    /// Verifies that TryGet() correctly deserializes nested objects within wrapper objects,
    /// maintaining the hierarchical structure.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesNestedObjects()
    {
        // Arrange
        var jsonData = @"{
            ""User"": {
                ""Name"": ""John"",
                ""Age"": 35
            },
            ""Status"": ""active""
        }";
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse(jsonData)!.AsObject()
        };

        // Act
        var success = result.TryGet<UserWrapper>(out var wrapper);

        // Assert
        Assert.True(success);
        Assert.NotNull(wrapper);
        Assert.Equal("active", wrapper.Status);
        Assert.NotNull(wrapper.User);
        Assert.Equal("John", wrapper.User.Name);
        Assert.Equal(35, wrapper.User.Age);
    }

    /// <summary>
    /// Verifies that TryGet() correctly deserializes boolean values (true/false).
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesBooleanValues()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse("{\"IsActive\":true,\"IsVerified\":false}")!.AsObject()
        };

        // Act
        var success = result.TryGet<StatusObject>(out var status);

        // Assert
        Assert.True(success);
        Assert.NotNull(status);
        Assert.True(status.IsActive);
        Assert.False(status.IsVerified);
    }

    /// <summary>
    /// Verifies that TryGet() correctly deserializes various numeric types
    /// (int, double, long) with appropriate precision.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode("Test")]
    [RequiresDynamicCode("Test")]
    public void TryGet_HandlesNumericTypes()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = JsonNode.Parse("{\"IntValue\":42,\"DoubleValue\":3.14,\"LongValue\":9223372036854775807}")!.AsObject()
        };

        // Act
        var success = result.TryGet<NumericObject>(out var numeric);

        // Assert
        Assert.True(success);
        Assert.NotNull(numeric);
        Assert.Equal(42, numeric.IntValue);
        Assert.Equal(3.14, numeric.DoubleValue);
        Assert.Equal(9223372036854775807, numeric.LongValue);
    }

    /// <summary>
    /// Verifies that both Result and Error properties can be null simultaneously,
    /// which may occur in certain edge cases or incomplete responses.
    /// </summary>
    [Fact]
    public void ResultAndError_CanBothBeNull()
    {
        // Arrange & Act
        var result = new JsonRpcClientResult
        {
            Result = null,
            Error = null
        };

        // Assert
        Assert.Null(result.Result);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Verifies that both Result and Error properties can be set simultaneously,
    /// though this violates JSON-RPC 2.0 spec (should have one or the other, not both).
    /// </summary>
    [Fact]
    public void ResultAndError_CanBothBeSet()
    {
        // Arrange
        var result = new JsonRpcClientResult
        {
            Result = new JsonObject { ["data"] = "test" },
            Error = new JsonObject { ["code"] = -1 }
        };

        // Assert
        Assert.NotNull(result.Result);
        Assert.NotNull(result.Error);
    }

    // Test helper classes
    /// <summary>
    /// Simple test class with basic properties for deserialization testing.
    /// </summary>
    private class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    /// <summary>
    /// Test class representing an address with street and city properties.
    /// </summary>
    private class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test class with nested object property for complex deserialization testing.
    /// </summary>
    private class ComplexUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Address? Address { get; set; }
    }

    /// <summary>
    /// Test class with array property for array deserialization testing.
    /// </summary>
    private class ItemCollection
    {
        public int[] Items { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Test class that wraps another object for nested deserialization testing.
    /// </summary>
    private class UserWrapper
    {
        public TestUser? User { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test class with boolean properties for boolean deserialization testing.
    /// </summary>
    private class StatusObject
    {
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Test class with various numeric types for numeric deserialization testing.
    /// </summary>
    private class NumericObject
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public long LongValue { get; set; }
    }
}
