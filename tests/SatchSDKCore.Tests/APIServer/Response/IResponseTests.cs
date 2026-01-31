using System.Net;
using SSC.APIServer.Request;
using SSC.APIServer.Response;

namespace SSC.Tests.APIServer.Response;

/// <summary>
/// Tests for IResponse abstract class which serves as the base class for all response types.
/// Tests verify constructor validation, request storage, and type conversion properties.
/// </summary>
public class IResponseTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : IRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }

    /// <summary>
    /// Concrete implementation of IResponse for testing abstract class behavior.
    /// </summary>
    private class TestResponse : IResponse
    {
        public TestResponse(IRequest request) : base(request) { }
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestResponse(null!));
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
        var response = new TestResponse(mockRequest);

        // Assert
        Assert.NotNull(response.Request);
        Assert.Same(mockRequest, response.Request);
    }

    /// <summary>
    /// Verifies that Request property is read-only.
    /// </summary>
    [Fact]
    public void Request_PropertyIsReadOnly()
    {
        // Arrange
        var mockRequest1 = new MockRequest();
        var mockRequest2 = new MockRequest();
        var response = new TestResponse(mockRequest1);

        // Assert - Request should remain the original one
        Assert.Same(mockRequest1, response.Request);
        Assert.NotSame(mockRequest2, response.Request);
    }

    /// <summary>
    /// Verifies that AsRESTResponse returns null for non-REST response.
    /// </summary>
    [Fact]
    public void AsRESTResponse_WithNonRESTResponse_ReturnsNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act
        var asREST = response.AsRESTResponse;

        // Assert
        Assert.Null(asREST);
    }

    /// <summary>
    /// Verifies that AsRESTResponse returns self for RESTResponse instance.
    /// </summary>
    [Fact]
    public void AsRESTResponse_WithRESTResponse_ReturnsSelf()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var restResponse = new RESTResponse(mockRequest, HttpStatusCode.OK, null, null);

        // Act
        var asREST = restResponse.AsRESTResponse;

        // Assert
        Assert.NotNull(asREST);
        Assert.Same(restResponse, asREST);
    }

    /// <summary>
    /// Verifies that multiple response instances can be created with different requests.
    /// </summary>
    [Fact]
    public void Constructor_WithMultipleInstances_StoresIndependentRequests()
    {
        // Arrange
        var mockRequest1 = new MockRequest();
        var mockRequest2 = new MockRequest();

        // Act
        var response1 = new TestResponse(mockRequest1);
        var response2 = new TestResponse(mockRequest2);

        // Assert
        Assert.Same(mockRequest1, response1.Request);
        Assert.Same(mockRequest2, response2.Request);
        Assert.NotSame(response1.Request, response2.Request);
    }

    /// <summary>
    /// Verifies that IResponse is an abstract class.
    /// </summary>
    [Fact]
    public void IResponse_IsAbstractClass()
    {
        // Assert
        Assert.True(typeof(IResponse).IsAbstract);
    }

    /// <summary>
    /// Verifies that concrete implementations can inherit from IResponse.
    /// </summary>
    [Fact]
    public void ConcreteImplementation_CanInheritFromIResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act & Assert
        Assert.IsAssignableFrom<IResponse>(response);
    }

    /// <summary>
    /// Verifies that Request property cannot be null after construction.
    /// </summary>
    [Fact]
    public void Request_AfterConstruction_IsNotNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act & Assert
        Assert.NotNull(response.Request);
    }

    /// <summary>
    /// Verifies that AsRESTResponse property can be checked without exceptions.
    /// </summary>
    [Fact]
    public void AsRESTResponse_CanBeCheckedSafely()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act
        var asREST = response.AsRESTResponse;

        // Assert - Should not throw and should be null for non-REST response
        Assert.Null(asREST);
    }

    /// <summary>
    /// Verifies that IResponse has DynamicallyAccessedMembers attribute.
    /// </summary>
    [Fact]
    public void IResponse_HasDynamicallyAccessedMembersAttribute()
    {
        // Act
        var attribute = typeof(IResponse).GetCustomAttributes(
            typeof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute),
            false
        );

        // Assert
        Assert.NotEmpty(attribute);
    }
}
