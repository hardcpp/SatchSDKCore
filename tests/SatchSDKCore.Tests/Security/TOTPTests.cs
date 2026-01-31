using System.Text;
using SSC.Security;

namespace SSC.Tests.Security;

/// <summary>
/// Tests for TOTP (Time-based One-Time Password) class which implements
/// RFC 6238 TOTP algorithm using HMAC-SHA1.
/// </summary>
public class TOTPTests
{
    /// <summary>
    /// Verifies that ComputeCode generates a code with the correct number of digits.
    /// </summary>
    [Fact]
    public void ComputeCode_DefaultParameters_GeneratesSixDigitCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code = TOTP.ComputeCode(secret);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode generates a code with custom number of digits.
    /// </summary>
    [Fact]
    public void ComputeCode_EightDigits_GeneratesEightDigitCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code = TOTP.ComputeCode(secret, digits: 8);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode generates a code with custom number of digits (4).
    /// </summary>
    [Fact]
    public void ComputeCode_FourDigits_GeneratesFourDigitCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code = TOTP.ComputeCode(secret, digits: 4);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(4, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode pads with leading zeros when necessary.
    /// </summary>
    [Fact]
    public void ComputeCode_GeneratesCodeWithLeadingZeros_WhenNecessary()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("test_secret_with_zeros");

        // Act
        var code = TOTP.ComputeCode(secret, digits: 6);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.Matches(@"^\d{6}$", code); // Exactly 6 digits
    }

    /// <summary>
    /// Verifies that ComputeCode generates the same code for the same secret within the same time window.
    /// </summary>
    [Fact]
    public void ComputeCode_SameSecretSameTimeWindow_GeneratesSameCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code1 = TOTP.ComputeCode(secret);
        var code2 = TOTP.ComputeCode(secret);

        // Assert
        Assert.Equal(code1, code2);
    }

    /// <summary>
    /// Verifies that ComputeCode generates different codes for different secrets.
    /// </summary>
    [Fact]
    public void ComputeCode_DifferentSecrets_GeneratesDifferentCodes()
    {
        // Arrange
        var secret1 = Encoding.ASCII.GetBytes("12345678901234567890");
        var secret2 = Encoding.ASCII.GetBytes("09876543210987654321");

        // Act
        var code1 = TOTP.ComputeCode(secret1);
        var code2 = TOTP.ComputeCode(secret2);

        // Assert
        Assert.NotEqual(code1, code2);
    }

    /// <summary>
    /// Verifies that ComputeCode works with different period values.
    /// </summary>
    [Fact]
    public void ComputeCode_CustomPeriod_GeneratesValidCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code30 = TOTP.ComputeCode(secret, period: 30);
        var code60 = TOTP.ComputeCode(secret, period: 60);

        // Assert
        Assert.NotNull(code30);
        Assert.NotNull(code60);
        Assert.Equal(6, code30.Length);
        Assert.Equal(6, code60.Length);
        // Different periods may generate different codes
    }

    /// <summary>
    /// Verifies that ComputeCode works with windowOffset parameter.
    /// </summary>
    [Fact]
    public void ComputeCode_WithWindowOffset_GeneratesValidCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code0 = TOTP.ComputeCode(secret, windowOffset: 0);
        var code1 = TOTP.ComputeCode(secret, windowOffset: 1);
        var codeNeg1 = TOTP.ComputeCode(secret, windowOffset: -1);

        // Assert
        Assert.NotNull(code0);
        Assert.NotNull(code1);
        Assert.NotNull(codeNeg1);
        Assert.Equal(6, code0.Length);
        Assert.Equal(6, code1.Length);
        Assert.Equal(6, codeNeg1.Length);
        // Different window offsets should generate different codes
        Assert.NotEqual(code0, code1);
        Assert.NotEqual(code0, codeNeg1);
    }

    /// <summary>
    /// Verifies that ComputeCode works with binary secrets.
    /// </summary>
    [Fact]
    public void ComputeCode_BinarySecret_GeneratesValidCode()
    {
        // Arrange
        var secret = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                  0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

        // Act
        var code = TOTP.ComputeCode(secret);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode works with very short secrets.
    /// </summary>
    [Fact]
    public void ComputeCode_ShortSecret_GeneratesValidCode()
    {
        // Arrange
        var secret = new byte[] { 0x01, 0x02 };

        // Act
        var code = TOTP.ComputeCode(secret);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode works with very long secrets.
    /// </summary>
    [Fact]
    public void ComputeCode_LongSecret_GeneratesValidCode()
    {
        // Arrange
        var secret = new byte[256];
        for (int i = 0; i < secret.Length; i++)
            secret[i] = (byte)(i % 256);

        // Act
        var code = TOTP.ComputeCode(secret);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ForgeURL generates a valid OTPAuth URL with default parameters.
    /// </summary>
    [Fact]
    public void ForgeURL_DefaultParameters_GeneratesValidURL()
    {
        // Arrange
        var label = "user@example.com";
        var issuer = "Example App";
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("otpauth://totp/", url);
        Assert.Contains("user%40example.com", url); // URL encoded
        Assert.Contains("secret=", url);
        Assert.Contains("digits=6", url);
        Assert.Contains("period=30", url);
        Assert.Contains("issuer=Example+App", url); // URL encoded
    }

    /// <summary>
    /// Verifies that ForgeURL generates a valid OTPAuth URL with custom parameters.
    /// </summary>
    [Fact]
    public void ForgeURL_CustomParameters_GeneratesValidURL()
    {
        // Arrange
        var label = "testuser";
        var issuer = "TestIssuer";
        var secret = Encoding.ASCII.GetBytes("testsecret");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret, digits: 8, period: 60);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("otpauth://totp/", url);
        Assert.Contains("testuser", url);
        Assert.Contains("secret=", url);
        Assert.Contains("digits=8", url);
        Assert.Contains("period=60", url);
        Assert.Contains("issuer=TestIssuer", url);
    }

    /// <summary>
    /// Verifies that ForgeURL properly URL-encodes special characters in label.
    /// </summary>
    [Fact]
    public void ForgeURL_LabelWithSpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var label = "user+test@example.com";
        var issuer = "App";
        var secret = Encoding.ASCII.GetBytes("secret");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.Contains("user%2btest%40example.com", url); // + and @ should be encoded (lowercase hex)
    }

    /// <summary>
    /// Verifies that ForgeURL properly URL-encodes special characters in issuer.
    /// </summary>
    [Fact]
    public void ForgeURL_IssuerWithSpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var label = "user";
        var issuer = "My App & Service";
        var secret = Encoding.ASCII.GetBytes("secret");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.Contains("issuer=My+App+%26+Service", url); // Spaces and & should be encoded
    }

    /// <summary>
    /// Verifies that ForgeURL generates Base32-encoded secret in URL.
    /// </summary>
    [Fact]
    public void ForgeURL_Secret_IsBase32Encoded()
    {
        // Arrange
        var label = "user";
        var issuer = "App";
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.Contains("secret=", url);

        // Extract secret from URL
        var secretMatch = System.Text.RegularExpressions.Regex.Match(url, @"secret=([A-Z2-7=]+)");
        Assert.True(secretMatch.Success, "Secret should be present in URL");

        var base32Secret = secretMatch.Groups[1].Value;
        Assert.NotEmpty(base32Secret);
        Assert.Matches(@"^[A-Z2-7=]+$", base32Secret); // Valid Base32 characters
    }

    /// <summary>
    /// Verifies that ForgeURL handles empty label string.
    /// </summary>
    [Fact]
    public void ForgeURL_EmptyLabel_GeneratesURLWithEmptyLabel()
    {
        // Arrange
        var label = "";
        var issuer = "App";
        var secret = Encoding.ASCII.GetBytes("secret");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("otpauth://totp/?", url); // Empty label results in /? pattern
    }

    /// <summary>
    /// Verifies that ForgeURL handles empty issuer string.
    /// </summary>
    [Fact]
    public void ForgeURL_EmptyIssuer_GeneratesURLWithEmptyIssuer()
    {
        // Arrange
        var label = "user";
        var issuer = "";
        var secret = Encoding.ASCII.GetBytes("secret");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.Contains("issuer=", url);
    }

    /// <summary>
    /// Verifies that ForgeURL generates complete and well-formed URLs.
    /// </summary>
    [Fact]
    public void ForgeURL_GeneratedURL_IsWellFormed()
    {
        // Arrange
        var label = "alice@example.com";
        var issuer = "MyApp";
        var secret = Encoding.ASCII.GetBytes("JBSWY3DPEHPK3PXP");

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret, digits: 6, period: 30);

        // Assert
        Assert.NotNull(url);

        // Check structure
        Assert.Matches(@"^otpauth://totp/[^?]+\?secret=[A-Z2-7=]+&digits=\d+&period=\d+&issuer=.+$", url);
    }

    /// <summary>
    /// Verifies that ForgeURL works with binary secrets.
    /// </summary>
    [Fact]
    public void ForgeURL_BinarySecret_GeneratesValidURL()
    {
        // Arrange
        var label = "user";
        var issuer = "App";
        var secret = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        // Act
        var url = TOTP.ForgeURL(label, issuer, secret);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("otpauth://totp/", url);
        Assert.Contains("secret=", url);
    }

    /// <summary>
    /// Verifies that ComputeCode generates all numeric characters (0-9).
    /// </summary>
    [Fact]
    public void ComputeCode_GeneratedCode_ContainsOnlyDigits()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("test_secret_123456");

        // Act
        var code = TOTP.ComputeCode(secret);

        // Assert
        Assert.NotNull(code);
        Assert.All(code, c => Assert.True(char.IsDigit(c), $"Character '{c}' is not a digit"));
    }

    /// <summary>
    /// Verifies that ComputeCode with different digits parameter generates correct length.
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void ComputeCode_VariousDigitLengths_GeneratesCorrectLength(int digits)
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code = TOTP.ComputeCode(secret, digits: digits);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(digits, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that ComputeCode with various periods generates valid codes.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void ComputeCode_VariousPeriods_GeneratesValidCodes(int period)
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        // Act
        var code = TOTP.ComputeCode(secret, period: period);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    /// <summary>
    /// Verifies that the same configuration generates consistent codes.
    /// </summary>
    [Fact]
    public void ComputeCode_Consistency_MultipleCallsGenerateSameCode()
    {
        // Arrange
        var secret = Encoding.ASCII.GetBytes("consistency_test_secret");
        var codes = new List<string>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            codes.Add(TOTP.ComputeCode(secret, digits: 6, period: 30));
        }

        // Assert
        Assert.All(codes, code => Assert.Equal(codes[0], code));
    }
}
