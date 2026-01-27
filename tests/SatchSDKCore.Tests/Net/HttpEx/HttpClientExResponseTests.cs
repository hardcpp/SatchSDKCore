using System.Net;
using System.Text;
using SSC.Net.HttpEx;

namespace SatchSDKCore.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpClientExResponse class which encapsulates HTTP client response data
/// including status codes, body content, rate limiting information, and retry logic.
/// </summary>
public class HttpClientExResponseTests
{
    /// <summary>
    /// Verifies that the constructor properly initializes all properties when provided
    /// with explicit status code, reason phrase, and success flag parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithExplicitParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var statusCode = HttpStatusCode.OK;
        var reasonPhrase = "Success";
        var isSuccess = true;

        // Act
        var response = new HttpClientExResponse(statusCode, reasonPhrase, isSuccess);

        // Assert
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(reasonPhrase, response.ReasonPhrase);
        Assert.True(response.IsSuccessStatusCode);
        Assert.False(response.ShouldRetry);
        Assert.False(response.IsRateLimited);
    }

    /// <summary>
    /// Verifies that the constructor can extract properties from an HttpResponseMessage
    /// and properly initialize the HttpClientExResponse object.
    /// </summary>
    [Fact]
    public void Constructor_WithHttpResponseMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            ReasonPhrase = "OK"
        };

        // Act
        var response = new HttpClientExResponse(httpResponse);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("OK", response.ReasonPhrase);
        Assert.True(response.IsSuccessStatusCode);
        Assert.False(response.ShouldRetry);
    }

    /// <summary>
    /// Verifies that ShouldRetry returns true for 5xx server errors (which are typically transient)
    /// and false for other status codes including 2xx success and 4xx client errors.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    public void ShouldRetry_ReturnsCorrectValue_BasedOnStatusCode(HttpStatusCode statusCode, bool expectedShouldRetry)
    {
        // Arrange & Act
        var response = new HttpClientExResponse(statusCode, null, statusCode == HttpStatusCode.OK);

        // Assert
        Assert.Equal(expectedShouldRetry, response.ShouldRetry);
    }

    /// <summary>
    /// Verifies that IsRateLimited returns true when the status code is 429 (Too Many Requests).
    /// This is the standard HTTP status code for rate limiting.
    /// </summary>
    [Fact]
    public void IsRateLimited_ReturnsTrue_When429StatusCode()
    {
        // Arrange
        var response = new HttpClientExResponse((HttpStatusCode)429, "Too Many Requests", false);

        // Act & Assert
        Assert.True(response.IsRateLimited);
        Assert.False(response.IsSuccessStatusCode);
    }

    /// <summary>
    /// Verifies that IsRateLimited returns false for status codes other than 429.
    /// </summary>
    [Fact]
    public void IsRateLimited_ReturnsFalse_WhenNot429StatusCode()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);

        // Act & Assert
        Assert.False(response.IsRateLimited);
    }

    /// <summary>
    /// Verifies that DangerousSetBodyBytes correctly sets the response body bytes
    /// and that BodyString properly decodes them to a string.
    /// </summary>
    [Fact]
    public void DangerousSetBodyBytes_SetsBodyBytesCorrectly()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var bodyBytes = Encoding.UTF8.GetBytes("Test response body");

        // Act
        response.DangerousSetBodyBytes(bodyBytes);

        // Assert
        Assert.NotNull(response.BodyBytes);
        Assert.Equal(bodyBytes, response.BodyBytes);
        Assert.Equal("Test response body", response.BodyString);
    }

    /// <summary>
    /// Verifies that DangerousSetBodyBytes throws InvalidOperationException when attempting
    /// to set the body bytes after they have already been set. This prevents accidental overwrites.
    /// </summary>
    [Fact]
    public void DangerousSetBodyBytes_ThrowsException_WhenBodyAlreadySet()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var bodyBytes = Encoding.UTF8.GetBytes("First body");
        response.DangerousSetBodyBytes(bodyBytes);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes("Second body")));

        Assert.Contains("Can not alter HTTPClientResponse body after initial set", exception.Message);
    }

    /// <summary>
    /// Verifies that DangerousSetBodyBytes throws ArgumentNullException when passed null.
    /// </summary>
    [Fact]
    public void DangerousSetBodyBytes_ThrowsException_WhenNull()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response.DangerousSetBodyBytes(null!));
    }

    /// <summary>
    /// Verifies that BodyString returns null when no body bytes have been set.
    /// </summary>
    [Fact]
    public void BodyString_ReturnsNull_WhenBodyBytesIsNull()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);

        // Act & Assert
        Assert.Null(response.BodyString);
    }

    /// <summary>
    /// Verifies that BodyString returns an empty string when body bytes is an empty array.
    /// </summary>
    [Fact]
    public void BodyString_ReturnsEmptyString_WhenBodyBytesIsEmpty()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        response.DangerousSetBodyBytes(Array.Empty<byte>());

        // Act & Assert
        Assert.Equal(string.Empty, response.BodyString);
    }

    /// <summary>
    /// Verifies that BodyString correctly handles and removes UTF-8 BOM (Byte Order Mark) preamble
    /// when present at the beginning of the response body.
    /// </summary>
    [Fact]
    public void BodyString_HandlesUTF8Preamble_Correctly()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes("Test content");
        var bodyWithPreamble = preamble.Concat(content).ToArray();

        // Act
        response.DangerousSetBodyBytes(bodyWithPreamble);

        // Assert
        Assert.Equal("Test content", response.BodyString);
    }

    /// <summary>
    /// Verifies that BodyString caches the decoded string value after first access
    /// to avoid repeated UTF-8 decoding operations for performance.
    /// </summary>
    [Fact]
    public void BodyString_CachesValue_AfterFirstAccess()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var bodyBytes = Encoding.UTF8.GetBytes("Test content");
        response.DangerousSetBodyBytes(bodyBytes);

        // Act
        var firstAccess = response.BodyString;
        var secondAccess = response.BodyString;

        // Assert - Same reference indicates caching
        Assert.Same(firstAccess, secondAccess);
    }

    /// <summary>
    /// Verifies that TryAsJObject successfully parses valid JSON response body
    /// into a JObject for easy property access.
    /// </summary>
    [Fact]
    public void TryAsJObject_ReturnsTrue_WithValidJson()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var jsonBody = "{\"key\":\"value\",\"number\":42}";
        response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes(jsonBody));

        // Act
        var result = response.TryAsJObject(out var jObject);

        // Assert
        Assert.True(result);
        Assert.NotNull(jObject);
        Assert.Equal("value", jObject["key"]?.ToString());
        Assert.Equal(42, (int?)jObject["number"]);
    }

    /// <summary>
    /// Verifies that TryAsJObject returns false and null when the response body
    /// contains invalid JSON that cannot be parsed.
    /// </summary>
    [Fact]
    public void TryAsJObject_ReturnsFalse_WithInvalidJson()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes("Not valid JSON"));

        // Act
        var result = response.TryAsJObject(out var jObject);

        // Assert
        Assert.False(result);
        Assert.Null(jObject);
    }

    /// <summary>
    /// Verifies that TryGetObject successfully deserializes valid JSON response body
    /// into a strongly-typed object.
    /// </summary>
    [Fact]
    public void TryGetObject_ReturnsTrue_WithValidJson()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var jsonBody = "{\"Name\":\"Test\",\"Value\":123}";
        response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes(jsonBody));

        // Act
        var result = response.TryGetObject<TestObject>(out var obj);

        // Assert
        Assert.True(result);
        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
        Assert.Equal(123, obj.Value);
    }

    /// <summary>
    /// Verifies that TryGetObject returns false and null when the response body
    /// contains invalid JSON that cannot be deserialized.
    /// </summary>
    [Fact]
    public void TryGetObject_ReturnsFalse_WithInvalidJson()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        response.DangerousSetBodyBytes(Encoding.UTF8.GetBytes("Invalid JSON"));

        // Act
        var result = response.TryGetObject<TestObject>(out var obj);

        // Assert
        Assert.False(result);
        Assert.Null(obj);
    }

    /// <summary>
    /// Verifies that DangerousSetRateLimit correctly sets rate limiting information
    /// extracted from HTTP response headers (X-RateLimit-*).
    /// </summary>
    [Fact]
    public void DangerousSetRateLimit_SetsRateLimitInfo()
    {
        // Arrange
        var response = new HttpClientExResponse(HttpStatusCode.OK, "OK", true);
        var httpResponse = new HttpResponseMessage((HttpStatusCode)429);
        httpResponse.Headers.Add("X-RateLimit-Limit", "100");
        httpResponse.Headers.Add("X-RateLimit-Remaining", "50");
        httpResponse.Headers.Add("X-RateLimit-Reset", "1234567890");
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(httpResponse);

        // Act
        response.DangerousSetRateLimit(rateLimitInfo);

        // Assert
        Assert.NotNull(response.RateLimitInfo);
        Assert.Equal(100, response.RateLimitInfo.Limit);
        Assert.Equal(50, response.RateLimitInfo.Remaining);
    }

    /// <summary>
    /// Test helper class for JSON deserialization tests.
    /// </summary>
    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
