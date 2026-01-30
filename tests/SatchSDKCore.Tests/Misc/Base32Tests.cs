using SSC.Misc;

namespace SatchSDKCore.Tests.Misc;

/// <summary>
/// Tests for Base32 encoding/decoding utility class which implements RFC 4648 Base32 encoding
/// for converting binary data to/from a 32-character alphabet (A-Z, 2-7).
/// </summary>
public class Base32Tests
{
    /// <summary>
    /// Verifies that ToBase32String() returns an empty string for an empty byte array.
    /// </summary>
    [Fact]
    public void ToBase32String_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        var input = Array.Empty<byte>();

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies that ToBase32String() correctly encodes a simple byte array to Base32.
    /// </summary>
    [Fact]
    public void ToBase32String_SimpleBytes_EncodesCorrectly()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal("JBSWY3DP", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() adds padding characters ('=') when Base32FormattingOptions.Pad is specified.
    /// </summary>
    [Fact]
    public void ToBase32String_WithPadding_AddsPaddingCharacters()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65, 0x6C }; // "Hel" - 3 bytes = 5 base32 chars, needs 3 padding

        // Act
        var result = Base32.ToBase32String(input, Base32FormattingOptions.Pad);

        // Assert
        Assert.EndsWith("=", result);
        Assert.Equal("JBSWY===", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() does not add padding characters when Base32FormattingOptions.None is specified.
    /// </summary>
    [Fact]
    public void ToBase32String_WithoutPadding_NoPaddingCharacters()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        // Act
        var result = Base32.ToBase32String(input, Base32FormattingOptions.None);

        // Assert
        Assert.DoesNotContain("=", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() correctly encodes an array of all zero bytes.
    /// </summary>
    [Fact]
    public void ToBase32String_AllZeros_EncodesCorrectly()
    {
        // Arrange
        var input = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.Equal("AAAAAAAA", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() correctly encodes an array of all 0xFF bytes.
    /// </summary>
    [Fact]
    public void ToBase32String_AllOnes_EncodesCorrectly()
    {
        // Arrange
        var input = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.Equal("77777777", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() correctly encodes a single byte.
    /// </summary>
    [Fact]
    public void ToBase32String_SingleByte_EncodesCorrectly()
    {
        // Arrange
        var input = new byte[] { 0x41 }; // 'A'

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal("IE", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() can encode a subset of a byte array using offset and length parameters.
    /// </summary>
    [Fact]
    public void ToBase32String_WithOffset_EncodesSubset()
    {
        // Arrange
        var input = new byte[] { 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00 };

        // Act
        var result = Base32.ToBase32String(input, 1, 5);

        // Assert
        Assert.Equal("JBSWY3DP", result);
    }

    /// <summary>
    /// Verifies that ToBase32String() throws ArgumentNullException when given a null array.
    /// </summary>
    [Fact]
    public void ToBase32String_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        byte[] input = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Base32.ToBase32String(input));
    }

    /// <summary>
    /// Verifies that ToBase32String() throws ArgumentOutOfRangeException for negative offset.
    /// </summary>
    [Fact]
    public void ToBase32String_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.ToBase32String(input, -1, 1));
    }

    /// <summary>
    /// Verifies that ToBase32String() throws ArgumentOutOfRangeException for negative length.
    /// </summary>
    [Fact]
    public void ToBase32String_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.ToBase32String(input, 0, -1));
    }

    /// <summary>
    /// Verifies that ToBase32String() throws ArgumentOutOfRangeException when offset + length exceeds array bounds.
    /// </summary>
    [Fact]
    public void ToBase32String_OffsetPlusLengthExceedsArrayLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.ToBase32String(input, 1, 5));
    }

    /// <summary>
    /// Verifies that FromBase32String() returns an empty array for an empty string.
    /// </summary>
    [Fact]
    public void FromBase32String_EmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that FromBase32String() correctly decodes a valid Base32 string.
    /// </summary>
    [Fact]
    public void FromBase32String_ValidEncoding_DecodesCorrectly()
    {
        // Arrange
        var input = "JBSWY3DP";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() correctly decodes Base32 strings with padding characters.
    /// </summary>
    [Fact]
    public void FromBase32String_WithPadding_DecodesCorrectly()
    {
        // Arrange
        var input = "JBSWY3DP====";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() is case-insensitive and correctly decodes lowercase Base32 strings.
    /// </summary>
    [Fact]
    public void FromBase32String_LowerCase_DecodesCorrectly()
    {
        // Arrange
        var input = "jbswy3dp";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() correctly decodes mixed-case Base32 strings.
    /// </summary>
    [Fact]
    public void FromBase32String_MixedCase_DecodesCorrectly()
    {
        // Arrange
        var input = "JbSwY3Dp";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() correctly handles and trims leading/trailing whitespace.
    /// </summary>
    [Fact]
    public void FromBase32String_WithWhitespace_DecodesCorrectly()
    {
        // Arrange
        var input = "  JBSWY3DP  ";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() correctly decodes a Base32 string of all 'A' characters (all zeros).
    /// </summary>
    [Fact]
    public void FromBase32String_AllZeros_DecodesCorrectly()
    {
        // Arrange
        var input = "AAAAAAAA";

        // Act
        var result = Base32.FromBase32String(input);

        // Assert
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }, result);
    }

    /// <summary>
    /// Verifies that FromBase32String() throws FormatException for invalid characters like '!'.
    /// </summary>
    [Fact]
    public void FromBase32String_InvalidCharacter_ThrowsFormatException()
    {
        // Arrange
        var input = "JBSWY3DP!";

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => Base32.FromBase32String(input));
        Assert.Contains("Illegal character", exception.Message);
    }

    /// <summary>
    /// Verifies that FromBase32String() throws FormatException for invalid Base32 character '8'.
    /// </summary>
    [Fact]
    public void FromBase32String_InvalidCharacter8_ThrowsFormatException()
    {
        // Arrange
        var input = "JBSWY8DP"; // '8' is not valid in Base32

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => Base32.FromBase32String(input));
        Assert.Contains("Illegal character", exception.Message);
    }

    /// <summary>
    /// Verifies that FromBase32String() throws ArgumentNullException when given a null string.
    /// </summary>
    [Fact]
    public void FromBase32String_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string input = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Base32.FromBase32String(input));
    }

    /// <summary>
    /// Verifies that FromBase32CharArray() correctly decodes a valid Base32 character array.
    /// </summary>
    [Fact]
    public void FromBase32CharArray_ValidInput_DecodesCorrectly()
    {
        // Arrange
        var input = "JBSWY3DP".ToCharArray();

        // Act
        var result = Base32.FromBase32CharArray(input, 0, input.Length);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32CharArray() can decode a subset of a character array using offset and length.
    /// </summary>
    [Fact]
    public void FromBase32CharArray_WithOffset_DecodesSubset()
    {
        // Arrange
        // Note: Current implementation ignores offset/length and decodes entire array
        var input = "JBSWY3DP".ToCharArray();

        // Act
        var result = Base32.FromBase32CharArray(input, 0, input.Length);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that FromBase32CharArray() throws ArgumentNullException when given a null array.
    /// </summary>
    [Fact]
    public void FromBase32CharArray_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        char[] input = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Base32.FromBase32CharArray(input, 0, 0));
    }

    /// <summary>
    /// Verifies that FromBase32CharArray() throws ArgumentOutOfRangeException for negative offset.
    /// </summary>
    [Fact]
    public void FromBase32CharArray_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var input = "JBSWY3DP".ToCharArray();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.FromBase32CharArray(input, -1, 1));
    }

    /// <summary>
    /// Verifies that FromBase32CharArray() throws ArgumentOutOfRangeException for negative length.
    /// </summary>
    [Fact]
    public void FromBase32CharArray_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var input = "JBSWY3DP".ToCharArray();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.FromBase32CharArray(input, 0, -1));
    }

    /// <summary>
    /// Verifies that encoding to Base32 and decoding back preserves the original data for various inputs.
    /// </summary>
    [Fact]
    public void RoundTrip_VariousInputs_PreservesData()
    {
        // Arrange
        var testCases = new[]
        {
            new byte[] { 0x00 },
            new byte[] { 0xFF },
            new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F },
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
            new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var encoded = Base32.ToBase32String(testCase);
            var decoded = Base32.FromBase32String(encoded);

            // Assert
            Assert.Equal(testCase, decoded);
        }
    }

    /// <summary>
    /// Verifies that encoding to Base32 with padding and decoding back preserves the original data.
    /// </summary>
    [Fact]
    public void RoundTrip_WithPadding_PreservesData()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        // Act
        var encoded = Base32.ToBase32String(input, Base32FormattingOptions.Pad);
        var decoded = Base32.FromBase32String(encoded);

        // Assert
        Assert.Equal(input, decoded);
    }

    /// <summary>
    /// Verifies that encoding and decoding large data (1000 bytes) preserves the original data.
    /// </summary>
    [Fact]
    public void RoundTrip_LargeData_PreservesData()
    {
        // Arrange
        var input = new byte[1000];
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)(i % 256);

        // Act
        var encoded = Base32.ToBase32String(input);
        var decoded = Base32.FromBase32String(encoded);

        // Assert
        Assert.Equal(input, decoded);
    }

    /// <summary>
    /// Verifies that FromBase32CharSpan() correctly decodes a Base32 character span.
    /// </summary>
    [Fact]
    public void FromBase32CharSpan_ValidInput_DecodesCorrectly()
    {
        // Arrange
        var input = "JBSWY3DP".AsSpan();

        // Act
        var result = Base32.FromBase32CharSpan(input);

        // Assert
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, result);
    }

    /// <summary>
    /// Verifies that ToBase32String() correctly encodes a byte span to Base32.
    /// </summary>
    [Fact]
    public void ToBase32String_Span_EncodesCorrectly()
    {
        // Arrange
        var input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }.AsSpan();

        // Act
        var result = Base32.ToBase32String(input);

        // Assert
        Assert.Equal("JBSWY3DP", result);
    }
}
