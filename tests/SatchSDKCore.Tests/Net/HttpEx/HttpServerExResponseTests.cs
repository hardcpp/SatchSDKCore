using System.Net;
using System.Net.Http;
using System.Text;
using SSC.Net.HttpEx;

namespace SatchSDKCore.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpServerExResponse class which encapsulates HTTP response data
/// including status code, content, and encoding for server responses.
/// </summary>
public class HttpServerExResponseTests
{
    /// <summary>
    /// Verifies that the constructor properly initializes all properties
    /// when provided with valid status code, content, and encoding.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var statusCode = HttpStatusCode.OK;
        var content = new StringContent("Test content", Encoding.UTF8, "application/json");
        var encoding = Encoding.UTF8;

        // Act
        var response = new HttpServerExResponse(statusCode, content, encoding);

        // Assert
        Assert.Equal(statusCode, response.Code);
        Assert.Equal(content, response.Content);
        Assert.Equal(encoding, response.ContentEncoding);
    }

    /// <summary>
    /// Verifies that the constructor correctly handles null content,
    /// which is valid for responses like 204 No Content.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContent_SetsContentToNull()
    {
        // Arrange
        var statusCode = HttpStatusCode.NoContent;

        // Act
        var response = new HttpServerExResponse(statusCode, null, null);

        // Assert
        Assert.Equal(statusCode, response.Code);
        Assert.Null(response.Content);
        Assert.Null(response.ContentEncoding);
    }

    /// <summary>
    /// Verifies that the constructor correctly handles null encoding,
    /// allowing the content to use its default encoding.
    /// </summary>
    [Fact]
    public void Constructor_WithNullEncoding_SetsEncodingToNull()
    {
        // Arrange
        var statusCode = HttpStatusCode.OK;
        var content = new StringContent("Test");

        // Act
        var response = new HttpServerExResponse(statusCode, content, null);

        // Assert
        Assert.Equal(statusCode, response.Code);
        Assert.Equal(content, response.Content);
        Assert.Null(response.ContentEncoding);
    }

    /// <summary>
    /// Verifies that the constructor correctly sets the status code
    /// for various HTTP status codes (2xx, 4xx, 5xx).
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void Constructor_WithDifferentStatusCodes_SetsCodeCorrectly(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var response = new HttpServerExResponse(statusCode, null, null);

        // Assert
        Assert.Equal(statusCode, response.Code);
    }

    /// <summary>
    /// Verifies that TryWrite returns false with an appropriate error message
    /// when attempting to write to a null HttpListenerResponse.
    /// </summary>
    [Fact]
    public void TryWrite_WithNullHttpResponse_ReturnsFalseWithError()
    {
        // Arrange
        var response = new HttpServerExResponse(HttpStatusCode.OK, null, null);

        // Act
        var result = response.TryWrite(null!, out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("No valid HttpListenerReponse", error);
    }

    /// <summary>
    /// Verifies that string content is properly preserved when creating a response.
    /// The content should be readable and match the original string.
    /// </summary>
    [Fact]
    public async Task Constructor_WithStringContent_PreservesContent()
    {
        // Arrange
        var testString = "Hello, World!";
        var content = new StringContent(testString, Encoding.UTF8, "text/plain");

        // Act
        var response = new HttpServerExResponse(HttpStatusCode.OK, content, Encoding.UTF8);

        // Assert
        Assert.NotNull(response.Content);
        var contentString = await response.Content.ReadAsStringAsync();
        Assert.Equal(testString, contentString);
    }

    /// <summary>
    /// Verifies that binary content (byte array) is properly preserved when creating a response.
    /// The content should be readable and match the original byte array.
    /// </summary>
    [Fact]
    public async Task Constructor_WithByteArrayContent_PreservesContent()
    {
        // Arrange
        var testBytes = Encoding.UTF8.GetBytes("Binary data");
        var content = new ByteArrayContent(testBytes);

        // Act
        var response = new HttpServerExResponse(HttpStatusCode.OK, content, null);

        // Assert
        Assert.NotNull(response.Content);
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(testBytes, contentBytes);
    }

    /// <summary>
    /// Verifies that the constructor correctly handles different text encodings
    /// (UTF-8, ASCII, Unicode) and preserves the encoding information.
    /// </summary>
    [Theory]
    [InlineData("UTF-8")]
    [InlineData("ASCII")]
    [InlineData("Unicode")]
    public void Constructor_WithDifferentEncodings_SetsEncodingCorrectly(string encodingName)
    {
        // Arrange
        var encoding = Encoding.GetEncoding(encodingName);
        var content = new StringContent("Test", encoding);

        // Act
        var response = new HttpServerExResponse(HttpStatusCode.OK, content, encoding);

        // Assert
        Assert.Equal(encoding, response.ContentEncoding);
    }

    /// <summary>
    /// Verifies that the constructor correctly handles empty string content,
    /// which is valid for responses that need to return an empty body.
    /// </summary>
    [Fact]
    public async Task Constructor_WithEmptyContent_HandlesCorrectly()
    {
        // Arrange
        var content = new StringContent(string.Empty, Encoding.UTF8);

        // Act
        var response = new HttpServerExResponse(HttpStatusCode.OK, content, Encoding.UTF8);

        // Assert
        Assert.NotNull(response.Content);
        var contentString = await response.Content.ReadAsStringAsync();
        Assert.Equal(string.Empty, contentString);
    }

    /// <summary>
    /// Verifies that JSON content preserves its content type header,
    /// which is important for API responses.
    /// </summary>
    [Fact]
    public void Constructor_WithJsonContent_PreservesContentType()
    {
        // Arrange
        var jsonContent = "{\"key\":\"value\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = new HttpServerExResponse(HttpStatusCode.OK, content, Encoding.UTF8);

        // Assert
        Assert.NotNull(response.Content);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Verifies that multiple HttpServerExResponse instances are independent
    /// and don't share state or interfere with each other.
    /// </summary>
    [Fact]
    public void Constructor_WithMultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var response1 = new HttpServerExResponse(
            HttpStatusCode.OK,
            new StringContent("Content 1"),
            Encoding.UTF8);

        var response2 = new HttpServerExResponse(
            HttpStatusCode.BadRequest,
            new StringContent("Content 2"),
            Encoding.ASCII);

        // Assert - Each instance should have its own independent state
        Assert.NotEqual(response1.Code, response2.Code);
        Assert.NotEqual(response1.Content, response2.Content);
        Assert.NotEqual(response1.ContentEncoding, response2.ContentEncoding);
    }
}
