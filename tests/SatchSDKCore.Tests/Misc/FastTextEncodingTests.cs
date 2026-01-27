using System;
using System.Text;
using SSC.Misc;

namespace SatchSDKCore.Tests.Misc;

/// <summary>
/// Tests for FastTextEncoding utility class which provides optimized text encoding operations
/// for UTF-8 and UTF-32 encodings using Span-based APIs for better performance.
/// </summary>
public class FastTextEncodingTests
{
    /// <summary>
    /// Verifies that GetByteCount() returns 0 for an empty string with UTF-8 encoding.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_EmptyString_ReturnsZero()
    {
        // Arrange
        var input = string.Empty.AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        Assert.Equal(0, count);
    }

    /// <summary>
    /// Verifies that GetByteCount() returns the correct byte count for a simple ASCII string with UTF-8 encoding.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_SimpleString_ReturnsCorrectCount()
    {
        // Arrange
        var input = "Hello".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        Assert.Equal(5, count);
    }

    /// <summary>
    /// Verifies that GetByteCount() correctly calculates byte count for multi-byte Unicode characters in UTF-8.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_UnicodeCharacters_ReturnsCorrectCount()
    {
        // Arrange
        var input = "Hello 世界".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        // "Hello " = 6 bytes, "世" = 3 bytes, "界" = 3 bytes = 12 total
        Assert.Equal(12, count);
    }

    /// <summary>
    /// Verifies that GetByteCount() correctly calculates byte count for emoji characters (4 bytes in UTF-8).
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_Emoji_ReturnsCorrectCount()
    {
        // Arrange
        var input = "Hello 😀".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        // "Hello " = 6 bytes, "😀" = 4 bytes = 10 total
        Assert.Equal(10, count);
    }

    /// <summary>
    /// Verifies that GetByteCount() returns the correct byte count for UTF-32 encoding (4 bytes per character).
    /// </summary>
    [Fact]
    public void GetByteCount_UTF32_SimpleString_ReturnsCorrectCount()
    {
        // Arrange
        var input = "Hello".AsSpan();
        var encoding = Encoding.UTF32;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        // UTF32 uses 4 bytes per character (for BMP characters)
        Assert.True(count >= 20); // At least 5 chars * 4 bytes
    }

    /// <summary>
    /// Verifies that GetByteCount() returns 0 for an empty string with UTF-32 encoding.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF32_EmptyString_ReturnsZero()
    {
        // Arrange
        var input = string.Empty.AsSpan();
        var encoding = Encoding.UTF32;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        Assert.Equal(0, count);
    }

    /// <summary>
    /// Verifies that GetByteCount() throws NotSupportedException for unsupported encodings like ASCII.
    /// </summary>
    [Fact]
    public void GetByteCount_UnsupportedEncoding_ThrowsNotSupportedException()
    {
        // Arrange
        var inputString = "Hello";
        var encoding = Encoding.ASCII;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            FastTextEncoding.GetByteCount(inputString.AsSpan(), encoding));
        Assert.Contains("not supported", exception.Message);
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes a simple ASCII string to UTF-8 bytes.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_SimpleString_EncodesCorrectly()
    {
        // Arrange
        var input = "Hello".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[10];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(5, bytesWritten);
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, buffer[..5]);
    }

    /// <summary>
    /// Verifies that GetBytes() writes nothing for an empty string with UTF-8 encoding.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_EmptyString_WritesNothing()
    {
        // Arrange
        var input = string.Empty.AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[10];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes multi-byte Unicode characters to UTF-8.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        var input = "世界".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[10];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(6, bytesWritten); // Each character is 3 bytes in UTF-8
        // Verify we can decode it back
        var decoded = Encoding.UTF8.GetString(buffer, 0, bytesWritten);
        Assert.Equal("世界", decoded);
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes emoji characters (4 bytes) to UTF-8.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_Emoji_EncodesCorrectly()
    {
        // Arrange
        var input = "😀".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[10];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(4, bytesWritten); // Emoji is 4 bytes in UTF-8
        // Verify we can decode it back
        var decoded = Encoding.UTF8.GetString(buffer, 0, bytesWritten);
        Assert.Equal("😀", decoded);
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes a simple string to UTF-32 bytes.
    /// </summary>
    [Fact]
    public void GetBytes_UTF32_SimpleString_EncodesCorrectly()
    {
        // Arrange
        var input = "Hello".AsSpan();
        var encoding = Encoding.UTF32;
        var buffer = new byte[100];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.True(bytesWritten >= 20); // At least 5 chars * 4 bytes
        // Verify we can decode it back
        var decoded = Encoding.UTF32.GetString(buffer, 0, bytesWritten);
        Assert.Equal("Hello", decoded);
    }

    /// <summary>
    /// Verifies that GetBytes() writes nothing for an empty string with UTF-32 encoding.
    /// </summary>
    [Fact]
    public void GetBytes_UTF32_EmptyString_WritesNothing()
    {
        // Arrange
        var input = string.Empty.AsSpan();
        var encoding = Encoding.UTF32;
        var buffer = new byte[10];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that GetBytes() throws NotSupportedException for unsupported encodings like ASCII.
    /// </summary>
    [Fact]
    public void GetBytes_UnsupportedEncoding_ThrowsNotSupportedException()
    {
        // Arrange
        var inputString = "Hello";
        var encoding = Encoding.ASCII;
        var buffer = new byte[10];

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            FastTextEncoding.GetBytes(inputString.AsSpan(), encoding, buffer));
        Assert.Contains("not supported", exception.Message);
    }

    /// <summary>
    /// Verifies that GetBytes() works correctly when the buffer is exactly the size needed.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_ExactBufferSize_Works()
    {
        // Arrange
        var input = "Hello".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[5]; // Exact size needed

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(5, bytesWritten);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    /// Verifies that GetByteCount() and GetBytes() return consistent byte counts for UTF-8 encoding.
    /// </summary>
    [Fact]
    public void GetByteCount_MatchesGetBytes_UTF8()
    {
        // Arrange
        var input = "Hello World! 世界 😀".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var expectedCount = FastTextEncoding.GetByteCount(input, encoding);
        var buffer = new byte[expectedCount];
        var actualCount = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }

    /// <summary>
    /// Verifies that GetByteCount() and GetBytes() return consistent byte counts for UTF-32 encoding.
    /// </summary>
    [Fact]
    public void GetByteCount_MatchesGetBytes_UTF32()
    {
        // Arrange
        var input = "Hello World!".AsSpan();
        var encoding = Encoding.UTF32;

        // Act
        var expectedCount = FastTextEncoding.GetByteCount(input, encoding);
        var buffer = new byte[expectedCount];
        var actualCount = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }

    /// <summary>
    /// Verifies that encoding to UTF-8 and decoding back preserves the original string data,
    /// including ASCII, Unicode, and emoji characters.
    /// </summary>
    [Fact]
    public void RoundTrip_UTF8_PreservesData()
    {
        // Arrange
        var original = "Hello World! 世界 😀";
        var encoding = Encoding.UTF8;
        var byteCount = FastTextEncoding.GetByteCount(original.AsSpan(), encoding);
        var buffer = new byte[byteCount];

        // Act
        FastTextEncoding.GetBytes(original.AsSpan(), encoding, buffer);
        var decoded = Encoding.UTF8.GetString(buffer);

        // Assert
        Assert.Equal(original, decoded);
    }

    /// <summary>
    /// Verifies that encoding to UTF-32 and decoding back preserves the original string data.
    /// </summary>
    [Fact]
    public void RoundTrip_UTF32_PreservesData()
    {
        // Arrange
        var original = "Hello World!";
        var encoding = Encoding.UTF32;
        var byteCount = FastTextEncoding.GetByteCount(original.AsSpan(), encoding);
        var buffer = new byte[byteCount];

        // Act
        FastTextEncoding.GetBytes(original.AsSpan(), encoding, buffer);
        var decoded = Encoding.UTF32.GetString(buffer);

        // Assert
        Assert.Equal(original, decoded);
    }

    /// <summary>
    /// Verifies that GetBytes() can handle large strings (1000 characters) efficiently with UTF-8 encoding.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_LargeString_EncodesCorrectly()
    {
        // Arrange
        var input = new string('A', 1000);
        var encoding = Encoding.UTF8;
        var buffer = new byte[1000];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input.AsSpan(), encoding, buffer);

        // Assert
        Assert.Equal(1000, bytesWritten);
        var decoded = Encoding.UTF8.GetString(buffer);
        Assert.Equal(input, decoded);
    }

    /// <summary>
    /// Verifies that GetByteCount() correctly handles special characters like newlines, tabs, and carriage returns.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_SpecialCharacters_ReturnsCorrectCount()
    {
        // Arrange
        var input = "Line1\nLine2\tTabbed\r\nWindows".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        var expected = Encoding.UTF8.GetByteCount(input);
        Assert.Equal(expected, count);
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes special characters like newlines, tabs, and carriage returns.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_SpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var input = "Line1\nLine2\tTabbed\r\nWindows".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[100];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        var decoded = Encoding.UTF8.GetString(buffer, 0, bytesWritten);
        Assert.Equal("Line1\nLine2\tTabbed\r\nWindows", decoded);
    }

    /// <summary>
    /// Verifies that GetByteCount() correctly handles null characters embedded in strings.
    /// </summary>
    [Fact]
    public void GetByteCount_UTF8_NullCharacter_HandlesCorrectly()
    {
        // Arrange
        var input = "Hello\0World".AsSpan();
        var encoding = Encoding.UTF8;

        // Act
        var count = FastTextEncoding.GetByteCount(input, encoding);

        // Assert
        Assert.Equal(11, count); // 5 + 1 (null) + 5
    }

    /// <summary>
    /// Verifies that GetBytes() correctly encodes null characters embedded in strings.
    /// </summary>
    [Fact]
    public void GetBytes_UTF8_NullCharacter_EncodesCorrectly()
    {
        // Arrange
        var input = "Hello\0World".AsSpan();
        var encoding = Encoding.UTF8;
        var buffer = new byte[20];

        // Act
        var bytesWritten = FastTextEncoding.GetBytes(input, encoding, buffer);

        // Assert
        Assert.Equal(11, bytesWritten);
        Assert.Equal(0, buffer[5]); // Null character
    }
}
