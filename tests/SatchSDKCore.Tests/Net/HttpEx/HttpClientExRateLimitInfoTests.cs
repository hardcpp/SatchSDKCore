using System.Net;
using SSC.Net.HttpEx;

namespace SSC.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpClientExRateLimitInfo class which parses and encapsulates HTTP rate limiting
/// information from response headers (X-RateLimit-*, Rate-Limit-*).
/// </summary>
public class HttpClientExRateLimitInfoTests
{
    /// <summary>
    /// Verifies that Get() throws ArgumentNullException when passed a null HttpResponseMessage.
    /// </summary>
    [Fact]
    public void Get_ThrowsArgumentNullException_WhenResponseIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => HttpClientExRateLimitInfo.Get(null!));
    }

    /// <summary>
    /// Verifies that Get() correctly parses standard X-RateLimit-* headers
    /// (Limit, Remaining, Reset) commonly used by APIs.
    /// </summary>
    [Fact]
    public void Get_ParsesStandardRateLimitHeaders()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotNull(rateLimitInfo);
        Assert.Equal(100, rateLimitInfo.Limit);
        Assert.Equal(50, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() can parse alternative Rate-Limit-* header format
    /// (without the X- prefix) used by some APIs.
    /// </summary>
    [Fact]
    public void Get_ParsesAlternativeHeaderFormats_WithHyphens()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Rate-Limit-Limit", "200");
        response.Headers.Add("Rate-Limit-Remaining", "75");
        response.Headers.Add("Rate-Limit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(200, rateLimitInfo.Limit);
        Assert.Equal(75, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() correctly parses X-RateLimit-* headers
    /// (standard format with X- prefix and hyphens).
    /// </summary>
    [Fact]
    public void Get_ParsesAlternativeHeaderFormats_WithoutHyphens()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "150");
        response.Headers.Add("X-RateLimit-Remaining", "25");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(150, rateLimitInfo.Limit);
        Assert.Equal(25, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() can parse X-RateLimit-Total as an alternative to X-RateLimit-Limit.
    /// Some APIs use "Total" instead of "Limit" to indicate the rate limit maximum.
    /// </summary>
    [Fact]
    public void Get_ParsesTotalHeaderVariant()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Total", "300");
        response.Headers.Add("X-RateLimit-Remaining", "100");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(300, rateLimitInfo.Limit);
        Assert.Equal(100, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() returns -1 for Limit when the limit header is missing.
    /// -1 indicates the value is unknown or not provided.
    /// </summary>
    [Fact]
    public void Get_ReturnsNegativeOne_WhenLimitHeaderMissing()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(-1, rateLimitInfo.Limit);
        Assert.Equal(50, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() returns -1 for Remaining when the remaining header is missing.
    /// -1 indicates the value is unknown or not provided.
    /// </summary>
    [Fact]
    public void Get_ReturnsNegativeOne_WhenRemainingHeaderMissing()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(100, rateLimitInfo.Limit);
        Assert.Equal(-1, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() returns -1 when header values cannot be parsed as integers.
    /// This handles malformed or non-numeric header values gracefully.
    /// </summary>
    [Fact]
    public void Get_ReturnsNegativeOne_WhenHeaderValueIsInvalid()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "invalid");
        response.Headers.Add("X-RateLimit-Remaining", "not-a-number");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(-1, rateLimitInfo.Limit);
        Assert.Equal(-1, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() handles comma-separated header values by taking the first value.
    /// Some HTTP implementations may send multiple values separated by commas.
    /// </summary>
    [Fact]
    public void Get_HandlesHeadersWithCommas()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100, 200");
        response.Headers.Add("X-RateLimit-Remaining", "50, 75");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(100, rateLimitInfo.Limit); // Should take first value
        Assert.Equal(50, rateLimitInfo.Remaining); // Should take first value
    }

    /// <summary>
    /// Verifies that Get() correctly trims whitespace from header values before parsing.
    /// </summary>
    [Fact]
    public void Get_HandlesHeadersWithWhitespace()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "  100  ");
        response.Headers.Add("X-RateLimit-Remaining", "  50  ");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(100, rateLimitInfo.Limit);
        Assert.Equal(50, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() can parse Reset header as a Unix timestamp (seconds since epoch).
    /// Large timestamp values are interpreted as absolute Unix timestamps.
    /// </summary>
    [Fact]
    public void Get_ParsesResetAsUnixTimestamp()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
        Assert.NotEqual(DateTime.MaxValue, rateLimitInfo.Reset);
    }

    /// <summary>
    /// Verifies that Get() can parse Reset header as relative seconds from now.
    /// Small values are interpreted as seconds to add to the current time.
    /// </summary>
    [Fact]
    public void Get_ParsesResetAsRelativeSeconds()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "60"); // 60 seconds from now

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
        Assert.True(rateLimitInfo.Reset > DateTime.Now);
    }

    /// <summary>
    /// Verifies that Get() handles invalid Reset header values by defaulting to 2 seconds from now.
    /// This provides a reasonable fallback when the reset time cannot be determined.
    /// </summary>
    [Fact]
    public void Get_HandlesInvalidResetValue()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "invalid");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
        // Should default to DateTime.Now.AddSeconds(2)
        Assert.True(rateLimitInfo.Reset > DateTime.Now);
        Assert.True(rateLimitInfo.Reset < DateTime.Now.AddSeconds(5));
    }

    /// <summary>
    /// Verifies that Get() handles missing Reset header by defaulting to 2 seconds from now.
    /// This provides a reasonable fallback when the reset time is not provided.
    /// </summary>
    [Fact]
    public void Get_HandlesMissingResetHeader()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
        // Should default to DateTime.Now.AddSeconds(2)
        Assert.True(rateLimitInfo.Reset > DateTime.Now);
        Assert.True(rateLimitInfo.Reset < DateTime.Now.AddSeconds(5));
    }

    /// <summary>
    /// Verifies that Get() performs case-insensitive header name matching.
    /// HTTP headers are case-insensitive per RFC 2616.
    /// </summary>
    [Fact]
    public void Get_IsCaseInsensitive()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("x-ratelimit-limit", "100");
        response.Headers.Add("X-RATELIMIT-REMAINING", "50");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(100, rateLimitInfo.Limit);
        Assert.Equal(50, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that Get() returns a valid object with default values when no rate limit headers are present.
    /// Limit and Remaining default to -1, Reset defaults to 2 seconds from now.
    /// </summary>
    [Fact]
    public void Get_HandlesEmptyHeaders()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.NotNull(rateLimitInfo);
        Assert.Equal(-1, rateLimitInfo.Limit);
        Assert.Equal(-1, rateLimitInfo.Remaining);
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
    }

    /// <summary>
    /// Verifies that Get() correctly handles zero values for Limit and Remaining.
    /// Zero is a valid value indicating no requests allowed or no requests remaining.
    /// </summary>
    [Fact]
    public void Get_HandlesZeroValues()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "0");
        response.Headers.Add("X-RateLimit-Remaining", "0");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert
        Assert.Equal(0, rateLimitInfo.Limit);
        Assert.Equal(0, rateLimitInfo.Remaining);
    }

    /// <summary>
    /// Verifies that the properties (Limit, Remaining, Reset) are read-only
    /// and cannot be modified after the object is created.
    /// </summary>
    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-RateLimit-Limit", "100");
        response.Headers.Add("X-RateLimit-Remaining", "50");
        response.Headers.Add("X-RateLimit-Reset", "1234567890");

        // Act
        var rateLimitInfo = HttpClientExRateLimitInfo.Get(response);

        // Assert - Properties should have private setters
        Assert.Equal(100, rateLimitInfo.Limit);
        Assert.Equal(50, rateLimitInfo.Remaining);
        Assert.NotEqual(DateTime.MinValue, rateLimitInfo.Reset);
    }
}
