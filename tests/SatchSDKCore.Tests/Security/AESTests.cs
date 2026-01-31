using System.Reflection;
using System.Text;
using SSC.Security;

namespace SSC.Tests.Security;

/// <summary>
/// Tests for AES encryption/decryption utility class which provides AES CBC encryption
/// with inline IV (Initialization Vector) functionality.
/// </summary>
public class AESTests
{
    // Use reflection to access internal AES class
    private static readonly Type? s_AesType;
    private static readonly MethodInfo? s_EncryptMethod;
    private static readonly MethodInfo? s_DecryptMethod;

    static AESTests()
    {
        var assembly = typeof(SSC.Misc.Base32).Assembly;
        s_AesType = assembly.GetType("SSC.Security.AES");

        if (s_AesType != null)
        {
            s_EncryptMethod = s_AesType.GetMethod("EncryptCBCInlineIV",
                BindingFlags.Public | BindingFlags.Static);
            s_DecryptMethod = s_AesType.GetMethod("DecryptCBCInlineIV",
                BindingFlags.Public | BindingFlags.Static);
        }
    }

    private static byte[]? EncryptCBCInlineIV(byte[] key, byte[] data)
    {
        if (s_EncryptMethod == null) return null;
        return (byte[]?)s_EncryptMethod.Invoke(null, new object[] { key, data });
    }

    private static byte[]? DecryptCBCInlineIV(byte[] key, byte[] data)
    {
        if (s_DecryptMethod == null) return null;
        return (byte[]?)s_DecryptMethod.Invoke(null, new object[] { key, data });
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV throws ArgumentException when key is null.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_NullKey_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        byte[] key = null!;
        var data = Encoding.UTF8.GetBytes("test data");

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => EncryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV throws ArgumentException when key is too short.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_ShortKey_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[] { 0x01, 0x02, 0x03 }; // Less than 6 bytes
        var data = Encoding.UTF8.GetBytes("test data");

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => EncryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains("Invalid key", ex.InnerException!.Message);
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV throws ArgumentException when data is null.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_NullData_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        byte[] data = null!;

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => EncryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV throws ArgumentException when data is empty.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_EmptyData_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        var data = Array.Empty<byte>();

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => EncryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains("Invalid data", ex.InnerException!.Message);
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV encrypts data successfully with 128-bit key.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_128BitKey_EncryptsSuccessfully()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        var data = Encoding.UTF8.GetBytes("Hello World");

        // Act
        var encrypted = EncryptCBCInlineIV(key, data);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.True(encrypted.Length > 16); // Should contain IV (16 bytes) + encrypted data
        Assert.NotEqual(data, encrypted);
    }

    /// <summary>
    /// Verifies that EncryptCBCInlineIV encrypts data successfully with 256-bit key.
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_256BitKey_EncryptsSuccessfully()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[32];
        for (int i = 0; i < key.Length; i++)
            key[i] = (byte)(i + 1);
        var data = Encoding.UTF8.GetBytes("Test data for 256-bit key");

        // Act
        var encrypted = EncryptCBCInlineIV(key, data);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.True(encrypted.Length > 16);
        Assert.NotEqual(data, encrypted);
    }

    /// <summary>
    /// Verifies that DecryptCBCInlineIV throws ArgumentException when key is null.
    /// </summary>
    [Fact]
    public void DecryptCBCInlineIV_NullKey_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        byte[] key = null!;
        var data = new byte[32]; // At least 17 bytes (16 for IV + 1 for data)

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => DecryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that DecryptCBCInlineIV throws ArgumentException when key is too short.
    /// </summary>
    [Fact]
    public void DecryptCBCInlineIV_ShortKey_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[] { 0x01, 0x02, 0x03 }; // Less than 6 bytes
        var data = new byte[32];

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => DecryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains("Invalid key", ex.InnerException!.Message);
    }

    /// <summary>
    /// Verifies that DecryptCBCInlineIV throws ArgumentException when data is null.
    /// </summary>
    [Fact]
    public void DecryptCBCInlineIV_NullData_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16];
        byte[] data = null!;

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => DecryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that DecryptCBCInlineIV throws ArgumentException when data is too short (less than IV size + 1).
    /// </summary>
    [Fact]
    public void DecryptCBCInlineIV_ShortData_ThrowsArgumentException()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16];
        var data = new byte[16]; // Exactly 16 bytes, need at least 17 (16 for IV + 1 for data)

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => DecryptCBCInlineIV(key, data));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains("Invalid data", ex.InnerException!.Message);
    }

    /// <summary>
    /// Verifies that data can be encrypted and then decrypted back to original value.
    /// </summary>
    [Fact]
    public void RoundTrip_EncryptThenDecrypt_RestoresOriginalData()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        var originalData = Encoding.UTF8.GetBytes("This is a test message for encryption");

        // Act
        var encrypted = EncryptCBCInlineIV(key, originalData);
        var decrypted = DecryptCBCInlineIV(key, encrypted!);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData, decrypted);
    }

    /// <summary>
    /// Verifies that encrypting the same data multiple times produces different ciphertext (due to random IV).
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_SameDataTwice_ProducesDifferentCiphertext()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        var data = Encoding.UTF8.GetBytes("Same data");

        // Act
        var encrypted1 = EncryptCBCInlineIV(key, data);
        var encrypted2 = EncryptCBCInlineIV(key, data);

        // Assert - IVs should be different, making ciphertext different
        Assert.NotNull(encrypted1);
        Assert.NotNull(encrypted2);
        Assert.NotEqual(encrypted1, encrypted2);
    }

    /// <summary>
    /// Verifies that round-trip encryption/decryption works with empty-like data.
    /// </summary>
    [Fact]
    public void RoundTrip_SingleByte_WorksCorrectly()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[32];
        for (int i = 0; i < key.Length; i++)
            key[i] = (byte)(i + 1);
        var originalData = new byte[] { 0xFF };

        // Act
        var encrypted = EncryptCBCInlineIV(key, originalData);
        var decrypted = DecryptCBCInlineIV(key, encrypted!);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData, decrypted);
    }

    /// <summary>
    /// Verifies that round-trip encryption/decryption works with large data.
    /// </summary>
    [Fact]
    public void RoundTrip_LargeData_WorksCorrectly()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16];
        for (int i = 0; i < key.Length; i++)
            key[i] = (byte)(i + 1);

        var originalData = new byte[10000];
        for (int i = 0; i < originalData.Length; i++)
            originalData[i] = (byte)(i % 256);

        // Act
        var encrypted = EncryptCBCInlineIV(key, originalData);
        var decrypted = DecryptCBCInlineIV(key, encrypted!);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData, decrypted);
    }

    /// <summary>
    /// Verifies that round-trip encryption/decryption works with special characters.
    /// </summary>
    [Fact]
    public void RoundTrip_SpecialCharacters_WorksCorrectly()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null || s_DecryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[24];
        for (int i = 0; i < key.Length; i++)
            key[i] = (byte)(255 - i);

        var originalData = Encoding.UTF8.GetBytes("Hello 世界! 🌍 Special chars: @#$%^&*()");

        // Act
        var encrypted = EncryptCBCInlineIV(key, originalData);
        var decrypted = DecryptCBCInlineIV(key, encrypted!);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData, decrypted);
        Assert.Equal("Hello 世界! 🌍 Special chars: @#$%^&*()", Encoding.UTF8.GetString(decrypted));
    }

    /// <summary>
    /// Verifies that encrypted data has IV prepended (first 16 bytes are the IV).
    /// </summary>
    [Fact]
    public void EncryptCBCInlineIV_EncryptedData_ContainsIVAtStart()
    {
        // Skip if AES type not accessible
        if (s_AesType == null || s_EncryptMethod == null)
        {
            Assert.Fail("AES class is not accessible for testing");
            return;
        }

        // Arrange
        var key = new byte[16];
        for (int i = 0; i < key.Length; i++)
            key[i] = (byte)(i + 1);
        var data = Encoding.UTF8.GetBytes("Test");

        // Act
        var encrypted = EncryptCBCInlineIV(key, data);

        // Assert
        Assert.NotNull(encrypted);
        Assert.True(encrypted.Length >= 16, "Encrypted data should have at least 16 bytes for IV");

        // IV should be first 16 bytes
        var iv = encrypted.Take(16).ToArray();
        Assert.NotNull(iv);
        Assert.Equal(16, iv.Length);
    }
}
