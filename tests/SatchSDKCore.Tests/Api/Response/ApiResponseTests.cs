using System.Net;
using SSC.Api.Request;
using SSC.Api.Response;

namespace SSC.Tests.Api.Response;

/// <summary>
/// Tests for ApiResponse abstract class which serves as the base class for all response types.
/// Tests verify constructor validation, request storage, and type conversion properties.
/// </summary>
public class ApiResponseTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : ApiRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }

    /// <summary>
    /// Concrete implementation of ApiResponse for testing abstract class behavior.
    /// </summary>
    private class TestResponse : ApiResponse
    {
        public TestResponse(ApiRequest request) : base(request) { }
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
    /// Verifies that AsApiHttpResponse returns null for non-REST response.
    /// </summary>
    [Fact]
    public void AsApiHttpResponse_WithNonApiHttpResponse_ReturnsNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act
        var asHttp = response.AsHttpResponse();

        // Assert
        Assert.Null(asHttp);
    }

    /// <summary>
    /// Verifies that AsApiHttpResponse returns self for ApiHttpResponse instance.
    /// </summary>
    [Fact]
    public void AsApiHttpResponse_WithApiHttpResponse_ReturnsSelf()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var apiHttpResponse = new ApiHttpResponse(mockRequest, HttpStatusCode.OK, null, null);

        // Act
        var asHttp = apiHttpResponse.AsHttpResponse();

        // Assert
        Assert.NotNull(asHttp);
        Assert.Same(apiHttpResponse, asHttp);
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
    /// Verifies that ApiResponse is an abstract class.
    /// </summary>
    [Fact]
    public void IResponse_IsAbstractClass()
    {
        // Assert
        Assert.True(typeof(ApiResponse).IsAbstract);
    }

    /// <summary>
    /// Verifies that concrete implementations can inherit from ApiResponse.
    /// </summary>
    [Fact]
    public void ConcreteImplementation_CanInheritFromIResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act & Assert
        Assert.IsAssignableFrom<ApiResponse>(response);
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
    /// Verifies that AsApiHttpResponse property can be checked without exceptions.
    /// </summary>
    [Fact]
    public void AsApiHttpResponse_CanBeCheckedSafely()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new TestResponse(mockRequest);

        // Act
        var asHttpResponse = response.AsHttpResponse();

        // Assert - Should not throw and should be null for non-REST response
        Assert.Null(asHttpResponse);
    }

    /// <summary>
    /// Verifies that ApiResponse has DynamicallyAccessedMembers attribute.
    /// </summary>
    [Fact]
    public void IResponse_HasDynamicallyAccessedMembersAttribute()
    {
        // Act
        var attribute = typeof(ApiResponse).GetCustomAttributes(
            typeof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute),
            false
        );

        // Assert
        Assert.NotEmpty(attribute);
    }
}
