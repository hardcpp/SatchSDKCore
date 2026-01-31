using System.Text;
using System.Text.Json.Nodes;
using SSC.Net.HttpEx;

namespace SSC.Tests.Net.HttpEx;

/// <summary>
/// Tests for HttpClientExPayload class which encapsulates HTTP request payload data
/// with support for form-encoded, JSON string, and JSON node payloads.
/// </summary>
public class HttpClientExPayloadTests
{
    /// <summary>
    /// Verifies that the Empty property returns a payload with empty bytes and empty content type.
    /// </summary>
    [Fact]
    public void Empty_ReturnsEmptyPayload()
    {
        // Act
        var payload = HttpClientExPayload.Empty;

        // Assert
        Assert.NotNull(payload);
        Assert.Empty(payload.Bytes);
        Assert.Equal(string.Empty, payload.Type);
    }

    /// <summary>
    /// Verifies that FromForm() creates a payload with the correct content type
    /// for application/x-www-form-urlencoded data.
    /// </summary>
    [Fact]
    public void FromForm_CreatesPayloadWithCorrectContentType()
    {
        // Arrange
        var formFields = new Dictionary<string, string>
        {
            { "username", "testuser" },
            { "password", "testpass" }
        };

        // Act
        var payload = HttpClientExPayload.FromForm(formFields);

        // Assert
        Assert.NotNull(payload);
        Assert.Equal("application/x-www-form-urlencoded", payload.Type);
        Assert.NotEmpty(payload.Bytes);
    }

    /// <summary>
    /// Verifies that FromForm() correctly encodes form fields as key=value pairs
    /// separated by ampersands (&).
    /// </summary>
    [Fact]
    public void FromForm_EncodesFieldsCorrectly()
    {
        // Arrange
        var formFields = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var payload = HttpClientExPayload.FromForm(formFields);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Contains("key1=value1", content);
        Assert.Contains("key2=value2", content);
        Assert.Contains("&", content);
    }

    /// <summary>
    /// Verifies that FromForm() properly URL-encodes special characters in form values.
    /// Characters like @ should be percent-encoded per RFC 3986.
    /// </summary>
    [Fact]
    public void FromForm_HandlesSpecialCharacters()
    {
        // Arrange
        var formFields = new Dictionary<string, string>
        {
            { "email", "test@example.com" },
            { "message", "Hello World!" }
        };

        // Act
        var payload = HttpClientExPayload.FromForm(formFields);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.NotNull(content);
        // URL encoding should handle special characters
        Assert.DoesNotContain("@", content); // @ should be encoded
    }

    /// <summary>
    /// Verifies that FromForm() handles an empty dictionary by creating a payload
    /// with empty content but correct content type.
    /// </summary>
    [Fact]
    public void FromForm_HandlesEmptyDictionary()
    {
        // Arrange
        var formFields = new Dictionary<string, string>();

        // Act
        var payload = HttpClientExPayload.FromForm(formFields);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Equal(string.Empty, content);
        Assert.Equal("application/x-www-form-urlencoded", payload.Type);
    }

    /// <summary>
    /// Verifies that FromForm() correctly handles a single field without adding
    /// unnecessary ampersands.
    /// </summary>
    [Fact]
    public void FromForm_HandlesSingleField()
    {
        // Arrange
        var formFields = new Dictionary<string, string>
        {
            { "token", "abc123" }
        };

        // Act
        var payload = HttpClientExPayload.FromForm(formFields);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Equal("token=abc123", content);
        Assert.DoesNotContain("&", content);
    }

    /// <summary>
    /// Verifies that FromJsonString() creates a payload with the correct JSON content type
    /// including UTF-8 charset specification.
    /// </summary>
    [Fact]
    public void FromJsonString_CreatesPayloadWithCorrectContentType()
    {
        // Arrange
        var jsonString = "{\"key\":\"value\"}";

        // Act
        var payload = HttpClientExPayload.FromJsonString(jsonString);

        // Assert
        Assert.NotNull(payload);
        Assert.Equal("application/json; charset=utf-8", payload.Type);
        Assert.NotEmpty(payload.Bytes);
    }

    /// <summary>
    /// Verifies that FromJsonString() preserves the exact JSON content without modification.
    /// </summary>
    [Fact]
    public void FromJsonString_PreservesJsonContent()
    {
        // Arrange
        var jsonString = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var payload = HttpClientExPayload.FromJsonString(jsonString);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Equal(jsonString, content);
    }

    /// <summary>
    /// Verifies that FromJsonString() correctly handles empty JSON objects.
    /// </summary>
    [Fact]
    public void FromJsonString_HandlesEmptyJson()
    {
        // Arrange
        var jsonString = "{}";

        // Act
        var payload = HttpClientExPayload.FromJsonString(jsonString);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Equal("{}", content);
    }

    /// <summary>
    /// Verifies that FromJsonString() correctly handles complex nested JSON structures
    /// with objects, arrays, and various data types.
    /// </summary>
    [Fact]
    public void FromJsonString_HandlesComplexJson()
    {
        // Arrange
        var jsonString = "{\"user\":{\"name\":\"John\",\"age\":30},\"active\":true}";

        // Act
        var payload = HttpClientExPayload.FromJsonString(jsonString);
        var content = Encoding.UTF8.GetString(payload.Bytes);

        // Assert
        Assert.Equal(jsonString, content);
    }

    /// <summary>
    /// Verifies that FromJson() creates a payload with the correct JSON content type
    /// when provided with a JsonNode object.
    /// </summary>
    [Fact]
    public void FromJson_CreatesPayloadWithCorrectContentType()
    {
        // Arrange
        var jsonNode = JsonNode.Parse("{\"key\":\"value\"}")!;

        // Act
        var payload = HttpClientExPayload.FromJson(jsonNode);

        // Assert
        Assert.NotNull(payload);
        Assert.Equal("application/json; charset=utf-8", payload.Type);
        Assert.NotEmpty(payload.Bytes);
    }

    /// <summary>
    /// Verifies that FromJson() correctly serializes a JsonObject and that the serialized
    /// content can be parsed back to retrieve the original values.
    /// </summary>
    [Fact]
    public void FromJson_SerializesJsonNodeCorrectly()
    {
        // Arrange
        var jsonObject = new JsonObject
        {
            ["name"] = "Test",
            ["value"] = 42,
            ["active"] = true
        };

        // Act
        var payload = HttpClientExPayload.FromJson(jsonObject);
        var content = Encoding.UTF8.GetString(payload.Bytes);
        var parsedBack = JsonNode.Parse(content);

        // Assert
        Assert.NotNull(parsedBack);
        Assert.Equal("Test", parsedBack["name"]?.GetValue<string>());
        Assert.Equal(42, parsedBack["value"]?.GetValue<int>());
        Assert.True(parsedBack["active"]?.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that FromJson() correctly handles nested JSON objects and preserves
    /// the hierarchical structure during serialization.
    /// </summary>
    [Fact]
    public void FromJson_HandlesNestedObjects()
    {
        // Arrange
        var jsonObject = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
                ["age"] = 30
            },
            ["active"] = true
        };

        // Act
        var payload = HttpClientExPayload.FromJson(jsonObject);
        var content = Encoding.UTF8.GetString(payload.Bytes);
        var parsedBack = JsonNode.Parse(content);

        // Assert
        Assert.NotNull(parsedBack);
        Assert.Equal("John", parsedBack["user"]?["name"]?.GetValue<string>());
        Assert.Equal(30, parsedBack["user"]?["age"]?.GetValue<int>());
        Assert.True(parsedBack["active"]?.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that FromJson() correctly serializes JSON arrays and preserves
    /// array elements and their order.
    /// </summary>
    [Fact]
    public void FromJson_HandlesArrays()
    {
        // Arrange
        var jsonObject = new JsonObject
        {
            ["items"] = new JsonArray(1, 2, 3, 4, 5)
        };

        // Act
        var payload = HttpClientExPayload.FromJson(jsonObject);
        var content = Encoding.UTF8.GetString(payload.Bytes);
        var parsedBack = JsonNode.Parse(content);

        // Assert
        Assert.NotNull(parsedBack);
        var items = parsedBack["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal(5, items.Count);
        Assert.Equal(1, items[0]?.GetValue<int>());
        Assert.Equal(5, items[4]?.GetValue<int>());
    }

    /// <summary>
    /// Verifies that the Bytes property can be accessed and returns the correct
    /// UTF-8 encoded byte array of the payload content.
    /// </summary>
    [Fact]
    public void Bytes_CanBeAccessed()
    {
        // Arrange
        var jsonString = "{\"test\":true}";
        var payload = HttpClientExPayload.FromJsonString(jsonString);

        // Act
        var bytes = payload.Bytes;

        // Assert
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        Assert.Equal(jsonString, Encoding.UTF8.GetString(bytes));
    }

    /// <summary>
    /// Verifies that the Type property can be accessed and returns the correct
    /// content type string for the payload.
    /// </summary>
    [Fact]
    public void Type_CanBeAccessed()
    {
        // Arrange
        var payload = HttpClientExPayload.FromJsonString("{}");

        // Act
        var type = payload.Type;

        // Assert
        Assert.Equal("application/json; charset=utf-8", type);
    }
}
