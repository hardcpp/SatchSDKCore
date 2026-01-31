using System.Net;
using SSC.Api.Request;
using SSC.Api.RouteContext;

namespace SSC.Tests.Api.RouteContext;

/// <summary>
/// Tests for ApiJsonRpcRouteContext class which represents a JSON-RPC route context
/// containing the request information, with support for storing additional context objects.
/// </summary>
public class JSONRPCRouteContextTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : ApiRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApiJsonRpcRouteContext(null!));
    }

    /// <summary>
    /// Verifies that constructor properly stores the request.
    /// </summary>
    [Fact]
    public void Constructor_WithValidRequest_StoresRequest()
    {
        // Arrange
        var mockRequest = new MockRequest();

        // Act
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Assert
        Assert.NotNull(context.Request);
        Assert.Same(mockRequest, context.Request);
    }

    /// <summary>
    /// Verifies that AsJSONRPCRouteContext returns itself.
    /// </summary>
    [Fact]
    public void AsJSONRPCRouteContext_ReturnsSelf()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        var asJSONRPC = context.AsJsonRpcRouteContext();

        // Assert
        Assert.NotNull(asJSONRPC);
        Assert.Same(context, asJSONRPC);
    }

    /// <summary>
    /// Verifies that AsRESTRouteContext returns null for JSON-RPC context.
    /// </summary>
    [Fact]
    public void AsRESTRouteContext_ReturnsNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        var asREST = context.AsHttpRouteContext();

        // Assert
        Assert.Null(asREST);
    }

    /// <summary>
    /// Verifies that AddObject and TryGetObject work correctly for storing and retrieving objects.
    /// </summary>
    [Fact]
    public void AddObject_TryGetObject_StoresAndRetrievesObject()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);
        var testObject = "TestValue";

        // Act
        context.AddObject("testKey", testObject);
        var result = context.TryGetObject<string>("testKey", out var retrievedObject);

        // Assert
        Assert.True(result);
        Assert.Equal(testObject, retrievedObject);
    }

    /// <summary>
    /// Verifies that TryGetObject returns false for non-existent key.
    /// </summary>
    [Fact]
    public void TryGetObject_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        var result = context.TryGetObject<string>("nonExistentKey", out var retrievedObject);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedObject);
    }

    /// <summary>
    /// Verifies that AddObject can store null values.
    /// </summary>
    [Fact]
    public void AddObject_WithNullValue_StoresNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        context.AddObject<string?>("nullKey", null);
        var result = context.TryGetObject<string?>("nullKey", out var retrievedObject);

        // Assert
        Assert.True(result);
        Assert.Null(retrievedObject);
    }

    /// <summary>
    /// Verifies that AddObject can store multiple objects with different keys.
    /// </summary>
    [Fact]
    public void AddObject_WithMultipleObjects_StoresAll()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        context.AddObject("key1", "value1");
        context.AddObject("key2", 42);
        context.AddObject("key3", true);

        // Assert
        Assert.True(context.TryGetObject<string>("key1", out var obj1));
        Assert.Equal("value1", obj1);

        Assert.True(context.TryGetObject<int>("key2", out var obj2));
        Assert.Equal(42, obj2);

        Assert.True(context.TryGetObject<bool>("key3", out var obj3));
        Assert.True(obj3);
    }

    /// <summary>
    /// Verifies that AddObject with duplicate key does not overwrite existing value.
    /// </summary>
    [Fact]
    public void AddObject_WithDuplicateKey_DoesNotOverwrite()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);
        context.AddObject("duplicateKey", "originalValue");

        // Act - Try to add with same key
        context.AddObject("duplicateKey", "newValue");

        // Assert - Should still have original value
        var result = context.TryGetObject<string>("duplicateKey", out var retrievedObject);
        Assert.True(result);
        Assert.Equal("originalValue", retrievedObject);
    }

    /// <summary>
    /// Verifies that TryGetObject returns false for wrong type.
    /// </summary>
    [Fact]
    public void TryGetObject_WithWrongType_ReturnsFalse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);
        context.AddObject("stringKey", "stringValue");

        // Act - Try to get as wrong type
        var result = context.TryGetObject<int>("stringKey", out var retrievedObject);

        // Assert
        Assert.False(result);
        Assert.Equal(0, retrievedObject);
    }

    /// <summary>
    /// Verifies that ApiJsonRpcRouteContext inherits from ApiRouteContext.
    /// </summary>
    [Fact]
    public void JSONRPCRouteContext_InheritsFromIRouteContext()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act & Assert
        Assert.IsAssignableFrom<ApiRouteContext>(context);
    }

    /// <summary>
    /// Verifies that Request property is read-only and cannot be changed.
    /// </summary>
    [Fact]
    public void Request_PropertyIsReadOnly()
    {
        // Arrange
        var mockRequest1 = new MockRequest();
        var mockRequest2 = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest1);

        // Assert - Request should be the original one and readonly
        Assert.Same(mockRequest1, context.Request);
        Assert.NotSame(mockRequest2, context.Request);
    }

    /// <summary>
    /// Verifies that object storage is independent per context instance.
    /// </summary>
    [Fact]
    public void AddObject_IndependentBetweenContexts()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context1 = new ApiJsonRpcRouteContext(mockRequest);
        var context2 = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        context1.AddObject("sharedKey", "value1");
        context2.AddObject("sharedKey", "value2");

        // Assert
        Assert.True(context1.TryGetObject<string>("sharedKey", out var obj1));
        Assert.Equal("value1", obj1);

        Assert.True(context2.TryGetObject<string>("sharedKey", out var obj2));
        Assert.Equal("value2", obj2);
    }

    /// <summary>
    /// Verifies that TryGetObject with empty objects returns false.
    /// </summary>
    [Fact]
    public void TryGetObject_BeforeAnyAddObject_ReturnsFalse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        var result = context.TryGetObject<string>("anyKey", out var retrievedObject);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedObject);
    }

    /// <summary>
    /// Verifies that AddObject handles value types correctly.
    /// </summary>
    [Fact]
    public void AddObject_WithValueTypes_StoresCorrectly()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);

        // Act
        context.AddObject("intKey", 123);
        context.AddObject("doubleKey", 45.67);
        context.AddObject("boolKey", false);

        // Assert
        Assert.True(context.TryGetObject<int>("intKey", out var intVal));
        Assert.Equal(123, intVal);

        Assert.True(context.TryGetObject<double>("doubleKey", out var doubleVal));
        Assert.Equal(45.67, doubleVal);

        Assert.True(context.TryGetObject<bool>("boolKey", out var boolVal));
        Assert.False(boolVal);
    }

    /// <summary>
    /// Verifies that ApiJsonRpcRouteContext and ApiHttpRouteContext are distinct types.
    /// </summary>
    [Fact]
    public void JSONRPCRouteContext_IsDistinctFromRESTRouteContext()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var jsonRpcContext = new ApiJsonRpcRouteContext(mockRequest);

        // Act & Assert
        Assert.NotNull(jsonRpcContext.AsJsonRpcRouteContext());
        Assert.Null(jsonRpcContext.AsHttpRouteContext());
    }

    /// <summary>
    /// Verifies that AddObject can store lists.
    /// </summary>
    [Fact]
    public void AddObject_WithList_StoresAndRetrieves()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);
        var list = new List<string> { "item1", "item2", "item3" };

        // Act
        context.AddObject("listKey", list);
        var result = context.TryGetObject<List<string>>("listKey", out var retrievedList);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrievedList);
        Assert.Equal(3, retrievedList.Count);
        Assert.Equal("item1", retrievedList[0]);
        Assert.Equal("item2", retrievedList[1]);
        Assert.Equal("item3", retrievedList[2]);
    }

    /// <summary>
    /// Verifies that AddObject can store dictionaries.
    /// </summary>
    [Fact]
    public void AddObject_WithDictionary_StoresAndRetrieves()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new ApiJsonRpcRouteContext(mockRequest);
        var dict = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 }
        };

        // Act
        context.AddObject("dictKey", dict);
        var result = context.TryGetObject<Dictionary<string, int>>("dictKey", out var retrievedDict);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrievedDict);
        Assert.Equal(3, retrievedDict.Count);
        Assert.Equal(1, retrievedDict["one"]);
        Assert.Equal(2, retrievedDict["two"]);
        Assert.Equal(3, retrievedDict["three"]);
    }
}
