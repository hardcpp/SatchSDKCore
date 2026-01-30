using Newtonsoft.Json.Linq;
using SSC.Reflection;

namespace SatchSDKCore.Tests.Reflection;

/// <summary>
/// Tests for TypeConverter which provides conversion utilities for converting JToken and string values
/// to various .NET primitive types and enums.
/// </summary>
public class TypeConverterTests
{
    /// <summary>
    /// Test enum for enum conversion tests
    /// </summary>
    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    #region TryGetValueAsFromJToken Tests

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken throws ArgumentNullException when type parameter is null.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type type = null!;
        var token = JToken.FromObject(42);
        object outValue = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TypeConverter.TryGetValueAsFromJToken(type, token, "test", out _, ref outValue));
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken throws ArgumentNullException when token parameter is null.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_NullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var type = typeof(int);
        JToken token = null!;
        object outValue = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TypeConverter.TryGetValueAsFromJToken(type, token, "test", out _, ref outValue));
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to ulong.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_ULong_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(ulong.MaxValue);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(ulong), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(ulong.MaxValue, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to long.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Long_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(-123456789L);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(long), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(-123456789L, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to uint.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_UInt_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(12345u);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(uint), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(12345u, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to int.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Int_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(-42);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(int), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(-42, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to ushort.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_UShort_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject((ushort)300);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(ushort), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((ushort)300, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to short.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Short_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject((short)-200);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(short), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((short)-200, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to byte.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Byte_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject((byte)255);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(byte), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((byte)255, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to sbyte.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_SByte_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject((sbyte)-100);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(sbyte), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((sbyte)-100, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to bool.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Bool_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(true);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(bool), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(true, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to string.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_String_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject("Hello World");
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(string), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("Hello World", outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to float.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Float_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(3.14f);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(float), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(3.14f, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken to double.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Double_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject(2.71828);
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(double), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(2.71828, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken correctly converts JToken string to enum.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_Enum_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject("Value2");
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(TestEnum), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(TestEnum.Value2, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken handles case-insensitive enum parsing.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_EnumCaseInsensitive_ConvertsCorrectly()
    {
        // Arrange
        var token = JToken.FromObject("value1");
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(TestEnum), token, "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(TestEnum.Value1, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken returns error for invalid enum value.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_InvalidEnumValue_ReturnsError()
    {
        // Arrange
        var token = JToken.FromObject("InvalidValue");
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(TestEnum), token, "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("Unrecognized constant", error);
        Assert.Contains("InvalidValue", error);
        Assert.Contains("testParam", error);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken returns error for wrong token type.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_WrongTokenType_ReturnsError()
    {
        // Arrange
        var token = JToken.FromObject("not a number");
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(int), token, "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("expected to be int", error);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromJToken returns error for unsupported type.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromJToken_UnsupportedType_ReturnsError()
    {
        // Arrange
        var token = JToken.FromObject(new { Value = 42 });
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromJToken(typeof(object), token, "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("Unhandled value type", error);
    }

    #endregion

    #region TryGetValueAsFromString Tests

    /// <summary>
    /// Verifies that TryGetValueAsFromString throws ArgumentNullException when type parameter is null.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type type = null!;
        object outValue = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TypeConverter.TryGetValueAsFromString(type, "42", "test", out _, ref outValue));
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString throws ArgumentNullException when input string is null.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var type = typeof(int);
        string input = null!;
        object outValue = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TypeConverter.TryGetValueAsFromString(type, input, "test", out _, ref outValue));
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to ulong.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_ULong_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(ulong), "18446744073709551615", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(ulong.MaxValue, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to long.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Long_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(long), "-123456789", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(-123456789L, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to uint.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_UInt_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(uint), "12345", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(12345u, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to int.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Int_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(int), "-42", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(-42, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to ushort.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_UShort_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(ushort), "300", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((ushort)300, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to short.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Short_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(short), "-200", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((short)-200, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to byte.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Byte_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(byte), "255", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((byte)255, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to sbyte.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_SByte_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(sbyte), "-100", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal((sbyte)-100, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to bool.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Bool_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(bool), "true", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(true, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to string.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_String_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(string), "Hello World", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("Hello World", outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to float with InvariantCulture.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Float_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(float), "3.14", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(3.14f, (float)outValue, 0.001f);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to double with InvariantCulture.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Double_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(double), "2.71828", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(2.71828, (double)outValue, 0.00001);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString correctly converts string to enum.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_Enum_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(TestEnum), "Value2", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(TestEnum.Value2, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString handles case-insensitive enum parsing.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_EnumCaseInsensitive_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(TestEnum), "value1", "test", out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(TestEnum.Value1, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString returns error for invalid enum value.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_InvalidEnumValue_ReturnsError()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(TestEnum), "InvalidValue", "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("Unrecognized constant", error);
        Assert.Contains("InvalidValue", error);
        Assert.Contains("testParam", error);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString returns error for invalid int string.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_InvalidInt_ReturnsError()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(int), "not a number", "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("expected to be int", error);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString returns error for unsupported type.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_UnsupportedType_ReturnsError()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(object), "test", "testParam", out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("Unhandled value type", error);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString handles null hint parameter correctly.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_NullHint_ConvertsCorrectly()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(int), "42", null, out var error, ref outValue);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(42, outValue);
    }

    /// <summary>
    /// Verifies that TryGetValueAsFromString error message includes empty string when hint is null.
    /// </summary>
    [Fact]
    public void TryGetValueAsFromString_NullHintWithError_IncludesEmptyString()
    {
        // Arrange
        object outValue = null!;

        // Act
        var result = TypeConverter.TryGetValueAsFromString(typeof(int), "invalid", null, out var error, ref outValue);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("expected to be int", error);
    }

    #endregion
}
