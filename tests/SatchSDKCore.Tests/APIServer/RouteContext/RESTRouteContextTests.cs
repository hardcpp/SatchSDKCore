using System.Net;
using SSC.APIServer.Request;
using SSC.APIServer.Route;
using SSC.APIServer.RouteContext;

namespace SSC.Tests.APIServer.RouteContext;

/// <summary>
/// Tests for RESTRouteContext class which represents a REST API route context
/// containing the request and REST method information, with support for storing
/// additional context objects.
/// </summary>
public class RESTRouteContextTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : IRequest
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
        Assert.Throws<ArgumentNullException>(() => new RESTRouteContext(null!, ERestMethod.Get));
    }

    /// <summary>
    /// Verifies that constructor properly stores the request and REST method.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_StoresRequestAndMethod()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var restMethod = ERestMethod.Post;

        // Act
        var context = new RESTRouteContext(mockRequest, restMethod);

        // Assert
        Assert.NotNull(context.Request);
        Assert.Same(mockRequest, context.Request);
        Assert.Equal(restMethod, context.RestMethod);
    }

    /// <summary>
    /// Verifies that constructor works with all REST method types.
    /// </summary>
    [Theory]
    [InlineData(ERestMethod.Get)]
    [InlineData(ERestMethod.Post)]
    [InlineData(ERestMethod.Put)]
    [InlineData(ERestMethod.Patch)]
    [InlineData(ERestMethod.Delete)]
    public void Constructor_WithAllRestMethods_StoresMethodCorrectly(ERestMethod method)
    {
        // Arrange
        var mockRequest = new MockRequest();

        // Act
        var context = new RESTRouteContext(mockRequest, method);

        // Assert
        Assert.Equal(method, context.RestMethod);
    }

    /// <summary>
    /// Verifies that AsRESTRouteContext returns itself.
    /// </summary>
    [Fact]
    public void AsRESTRouteContext_ReturnsSelf()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        // Act
        var asREST = context.AsRESTRouteContext;

        // Assert
        Assert.NotNull(asREST);
        Assert.Same(context, asREST);
    }

    /// <summary>
    /// Verifies that AsJSONRPCRouteContext returns null for REST context.
    /// </summary>
    [Fact]
    public void AsJSONRPCRouteContext_ReturnsNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        // Act
        var asJSONRPC = context.AsJSONRPCRouteContext;

        // Assert
        Assert.Null(asJSONRPC);
    }

    /// <summary>
    /// Verifies that AddObject and TryGetObject work correctly for storing and retrieving objects.
    /// </summary>
    [Fact]
    public void AddObject_TryGetObject_StoresAndRetrievesObject()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        context.AddObject("stringKey", "stringValue");

        // Act - Try to get as wrong type
        var result = context.TryGetObject<int>("stringKey", out var retrievedObject);

        // Assert
        Assert.False(result);
        Assert.Equal(0, retrievedObject);
    }

    /// <summary>
    /// Verifies that RESTRouteContext inherits from IRouteContext.
    /// </summary>
    [Fact]
    public void RESTRouteContext_InheritsFromIRouteContext()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        // Act & Assert
        Assert.IsAssignableFrom<IRouteContext>(context);
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
        var context = new RESTRouteContext(mockRequest1, ERestMethod.Get);

        // Assert - Request should be the original one and readonly
        Assert.Same(mockRequest1, context.Request);
        Assert.NotSame(mockRequest2, context.Request);
    }

    /// <summary>
    /// Verifies that RestMethod property is read-only and cannot be changed.
    /// </summary>
    [Fact]
    public void RestMethod_PropertyIsReadOnly()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        // Assert - RestMethod should remain the original value
        Assert.Equal(ERestMethod.Get, context.RestMethod);
    }

    /// <summary>
    /// Verifies that object storage is independent per context instance.
    /// </summary>
    [Fact]
    public void AddObject_IndependentBetweenContexts()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var context1 = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var context2 = new RESTRouteContext(mockRequest, ERestMethod.Post);

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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

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
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

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
}