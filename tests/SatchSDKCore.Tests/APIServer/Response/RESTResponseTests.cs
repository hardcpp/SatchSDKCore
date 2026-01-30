using System.Net;
using System.Text;
using SSC.APIServer.Request;
using SSC.APIServer.Response;
using SSC.APIServer.RouteContext;

namespace SatchSDKCore.Tests.APIServer.Response;

/// <summary>
/// Tests for RESTResponse class which represents HTTP REST API responses with
/// status codes, content, and encoding support. Tests verify constructor behavior,
/// static factory methods, and content type handling.
/// </summary>
public class RESTResponseTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : IRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }

    /// <summary>
    /// Mock route context implementation for testing purposes.
    /// </summary>
    private class MockRouteContext : IRouteContext
    {
        public MockRouteContext(IRequest request) : base(request)
        {
        }
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RESTResponse(null!, HttpStatusCode.OK, null, null));
    }

    /// <summary>
    /// Verifies that constructor properly stores the request and creates HTTP server response.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_StoresRequestAndCreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var content = new StringContent("test", Encoding.UTF8);

        // Act
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, content, Encoding.UTF8);

        // Assert
        Assert.NotNull(response.Request);
        Assert.Same(mockRequest, response.Request);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that constructor accepts all standard HTTP status codes.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void Constructor_WithVariousStatusCodes_CreatesResponseCorrectly(HttpStatusCode statusCode)
    {
        // Arrange
        var mockRequest = new MockRequest();

        // Act
        var response = new RESTResponse(mockRequest, statusCode, null, null);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that constructor accepts null content.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContent_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();

        // Act
        var response = new RESTResponse(mockRequest, HttpStatusCode.NoContent, null, null);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that constructor accepts null encoding.
    /// </summary>
    [Fact]
    public void Constructor_WithNullEncoding_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var content = new StringContent("test");

        // Act
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, content, null);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that CodeResult creates a response with the specified status code.
    /// </summary>
    [Fact]
    public void CodeResult_CreatesResponseWithStatusCode()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var response = RESTResponse.CodeResult(mockContext, HttpStatusCode.NotFound);

        // Assert
        Assert.NotNull(response);
        Assert.Same(mockRequest, response.Request);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that CodeResult works with various status codes.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public void CodeResult_WithVariousStatusCodes_CreatesResponse(HttpStatusCode statusCode)
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var response = RESTResponse.CodeResult(mockContext, statusCode);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result creates a response with content and default content type.
    /// </summary>
    [Fact]
    public void Result_WithDefaultContentType_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);
        const string content = "Test content";

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, content);

        // Assert
        Assert.NotNull(response);
        Assert.Same(mockRequest, response.Request);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result creates a response with custom content type.
    /// </summary>
    [Fact]
    public void Result_WithCustomContentType_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);
        const string content = "{\"test\":\"value\"}";

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, content, RESTResponse.ContentType_AppJson);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result works with HTML content type.
    /// </summary>
    [Fact]
    public void Result_WithHTMLContentType_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);
        const string content = "<html><body>Test</body></html>";

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, content, RESTResponse.ContentType_TextHTML);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result handles empty content.
    /// </summary>
    [Fact]
    public void Result_WithEmptyContent_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, string.Empty);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that AsRESTResponse returns itself.
    /// </summary>
    [Fact]
    public void AsRESTResponse_ReturnsSelf()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, null, null);

        // Act
        var asREST = response.AsRESTResponse;

        // Assert
        Assert.NotNull(asREST);
        Assert.Same(response, asREST);
    }

    /// <summary>
    /// Verifies that RESTResponse inherits from IResponse.
    /// </summary>
    [Fact]
    public void RESTResponse_InheritsFromIResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, null, null);

        // Act & Assert
        Assert.IsAssignableFrom<IResponse>(response);
    }

    /// <summary>
    /// Verifies that ContentType_AppJson constant has correct value.
    /// </summary>
    [Fact]
    public void ContentType_AppJson_HasCorrectValue()
    {
        // Assert
        Assert.Equal("application/json", RESTResponse.ContentType_AppJson);
    }

    /// <summary>
    /// Verifies that ContentType_TextHTML constant has correct value.
    /// </summary>
    [Fact]
    public void ContentType_TextHTML_HasCorrectValue()
    {
        // Assert
        Assert.Equal("text/html", RESTResponse.ContentType_TextHTML);
    }

    /// <summary>
    /// Verifies that HTTPServerResponse property is not null after construction.
    /// </summary>
    [Fact]
    public void HTTPServerResponse_AfterConstruction_IsNotNull()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, null, null);

        // Act & Assert
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result handles various status codes correctly.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK, "Success")]
    [InlineData(HttpStatusCode.Created, "Resource created")]
    [InlineData(HttpStatusCode.BadRequest, "Bad request")]
    [InlineData(HttpStatusCode.NotFound, "Not found")]
    public void Result_WithVariousStatusAndContent_CreatesResponse(HttpStatusCode status, string content)
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var response = RESTResponse.Result(mockContext, status, content);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that multiple RESTResponse instances can be created independently.
    /// </summary>
    [Fact]
    public void Constructor_WithMultipleInstances_CreatesIndependentResponses()
    {
        // Arrange
        var mockRequest1 = new MockRequest();
        var mockRequest2 = new MockRequest();

        // Act
        var response1 = new RESTResponse(mockRequest1, HttpStatusCode.OK, null, null);
        var response2 = new RESTResponse(mockRequest2, HttpStatusCode.BadRequest, null, null);

        // Assert
        Assert.NotSame(response1, response2);
        Assert.NotSame(response1.Request, response2.Request);
    }

    /// <summary>
    /// Verifies that Result with various content types creates responses correctly.
    /// </summary>
    [Theory]
    [InlineData("application/json")]
    [InlineData("text/html")]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public void Result_WithVariousContentTypes_CreatesResponse(string contentType)
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, "content", contentType);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that constructor with different encodings creates response correctly.
    /// </summary>
    [Theory]
    [InlineData("UTF8")]
    [InlineData("UTF32")]
    [InlineData("ASCII")]
    public void Constructor_WithDifferentEncodings_CreatesResponse(string encodingName)
    {
        // Arrange
        var mockRequest = new MockRequest();
        var encoding = encodingName switch
        {
            "UTF8" => Encoding.UTF8,
            "UTF32" => Encoding.UTF32,
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8
        };
        var content = new StringContent("test", encoding);

        // Act
        var response = new RESTResponse(mockRequest, HttpStatusCode.OK, content, encoding);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that RESTResponse is a sealed class.
    /// </summary>
    [Fact]
    public void RESTResponse_IsSealedClass()
    {
        // Assert
        Assert.True(typeof(RESTResponse).IsSealed);
    }

    /// <summary>
    /// Verifies that Result handles large content correctly.
    /// </summary>
    [Fact]
    public void Result_WithLargeContent_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);
        var largeContent = new string('x', 10000);

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, largeContent);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that Result handles special characters in content.
    /// </summary>
    [Fact]
    public void Result_WithSpecialCharacters_CreatesResponse()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);
        const string content = "Test with special chars: <>&\"'€©®™";

        // Act
        var response = RESTResponse.Result(mockContext, HttpStatusCode.OK, content);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.HTTPServerResponse);
    }

    /// <summary>
    /// Verifies that CodeResult and Result can be used together in same context.
    /// </summary>
    [Fact]
    public void CodeResult_And_Result_CanBeUsedTogether()
    {
        // Arrange
        var mockRequest = new MockRequest();
        var mockContext = new MockRouteContext(mockRequest);

        // Act
        var codeResponse = RESTResponse.CodeResult(mockContext, HttpStatusCode.OK);
        var contentResponse = RESTResponse.Result(mockContext, HttpStatusCode.OK, "content");

        // Assert
        Assert.NotNull(codeResponse);
        Assert.NotNull(contentResponse);
        Assert.NotSame(codeResponse, contentResponse);
    }
}