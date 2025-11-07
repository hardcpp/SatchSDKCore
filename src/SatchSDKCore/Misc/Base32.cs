using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace SSC.Misc;

[Flags]
public enum Base32FormattingOptions
{
    None = 0,
    Pad = 1
}

/// <summary>
/// Base32 encoding/decoding utilities
/// </summary>
public static class Base32
{
    private static readonly char[] m_Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    private const int c_Mask = 31;
    private const int c_Shift = 5;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Converts the specified string, which encodes binary data as Base32 digits, to the equivalent byte array.
    /// </summary>
    /// <param name="encoded">The string to convert</param>
    /// <returns>The array of bytes represented by the specified Base32 string.</returns>
    public static byte[] FromBase32String(string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);

        return FromBase32CharSpan(encoded.AsSpan());
    }
    /// <summary>
    /// Converts the specified range of a Char array, which encodes binary data as Base32 digits, to the equivalent byte array.
    /// </summary>
    /// <param name="inArray">Chars representing Base32 encoding characters</param>
    /// <param name="offset">A position within the input array</param>
    /// <param name="length">Number of element to convert</param>
    /// <returns>The array of bytes represented by the specified Base32 encoding characters.</returns>
    public static byte[] FromBase32CharArray(char[] inArray, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(inArray);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

        return FromBase32CharSpan(inArray.AsSpan());
    }
    /// <summary>
    /// Converts the specified span of Char, which encodes binary data as Base32 digits, to the equivalent byte array.
    /// </summary>
    /// <param name="charSpan">Chars span representing Base32 encdoing characters</param>
    /// <returns>The array of bytes represented by the specified Base32 encoding characters.</returns>
    /// <exception cref="FormatException">If an invalid Base32 digit is encountered</exception>
    public static byte[] FromBase32CharSpan(ReadOnlySpan<char> charSpan)
    {
        charSpan = charSpan.Trim().TrimEnd('=');
        if (charSpan.Length == 0)
            return Array.Empty<byte>();

        int l_OutLength     = charSpan.Length * c_Shift / 8;
        var l_Result        = new byte[l_OutLength];
        int l_BitsBuffer    = 0;
        int l_BitsRemaining = 0;
        int l_Next          = 0;
        int l_SymboNumber   = 0;

        for (var l_I = 0; l_I < charSpan.Length; ++l_I)
        {
            l_SymboNumber = SymbolToInt(char.ToUpper(charSpan[l_I]));
            if (l_SymboNumber < 0)
                throw new FormatException("Illegal character: `" + charSpan[l_I] + "`");

            l_BitsBuffer <<= c_Shift;
            l_BitsBuffer |= l_SymboNumber & c_Mask;
            l_BitsRemaining += c_Shift;

            if (l_BitsRemaining >= 8)
            {
                l_Result[l_Next++] = (byte)(l_BitsBuffer >> l_BitsRemaining - 8);
                l_BitsRemaining -= 8;
            }
        }

        return l_Result;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-32 digits
    /// </summary>
    /// <param name="inArray">An array of 8-bit unsigned integers</param>
    /// <param name="options">Encoding options</param>
    /// <returns>The string representation in base 32 of the elements in <paramref name="inArray"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32String(byte[] inArray, Base32FormattingOptions options = Base32FormattingOptions.None)
    {
        ArgumentNullException.ThrowIfNull(inArray);

        return ToBase32String(new ReadOnlySpan<byte>(inArray), options);
    }
    /// <summary>
    /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-32 digits.
    /// Parameters specify the subset as an offset in the input array, and the number of elements in the array to convert
    /// </summary>
    /// <param name="inArray">An array of 8-bit unsigned integers</param>
    /// <param name="offset">An offset in <paramref name="inArray"/></param>
    /// <param name="length">The number of elements of <paramref name="inArray"/> to convert</param>
    /// <param name="options">Encoding options</param>
    /// <returns>The string representation in base 32 of <paramref name="length"/> elements of <paramref name="inArray"/>, starting at position <paramref name="offset"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32String(byte[] inArray, int offset, int length, Base32FormattingOptions options = Base32FormattingOptions.None)
    {
        ArgumentNullException.ThrowIfNull(inArray);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

        return ToBase32String(new ReadOnlySpan<byte>(inArray, offset, length), options);
    }
    /// <summary>
    /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-32 digits.
    /// </summary>
    /// <param name="bytes">A read-only span of 8-bit unsigned integers</param>
    /// <param name="options">Encoding options</param>
    /// <returns>The string representation in base 32 of the elements in bytes. If the length of bytes is 0, an empty string is returned</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the input bytes are too big</exception>
    public static string ToBase32String(ReadOnlySpan<byte> bytes, Base32FormattingOptions options = Base32FormattingOptions.None)
    {
        if (bytes.Length == 0)
            return string.Empty;

        if (bytes.Length >= 1 << 28)
            throw new ArgumentOutOfRangeException(nameof(bytes));

        int l_OutputLength  = (bytes.Length * 8 + c_Shift - 1) / c_Shift;
        var l_Builder       = new StringBuilder(l_OutputLength);
        int l_Position      = 0;
        int l_LastPosition  = l_Position + bytes.Length;
        int l_BitsBuffer    = bytes[l_Position++];
        int l_BitsRemaining = 8;

        while (l_BitsRemaining > 0 || l_Position < l_LastPosition)
        {
            if (l_BitsRemaining < c_Shift)
            {
                if (l_Position < l_LastPosition)
                {
                    l_BitsBuffer <<= 8;
                    l_BitsBuffer |= bytes[l_Position++] & 0xFF;
                    l_BitsRemaining += 8;
                }
                else
                {
                    int l_Padding = c_Shift - l_BitsRemaining;
                    l_BitsBuffer <<= l_Padding;
                    l_BitsRemaining += l_Padding;
                }
            }

            int l_Index = c_Mask & l_BitsBuffer >> l_BitsRemaining - c_Shift;
            l_BitsRemaining -= c_Shift;

            l_Builder.Append(m_Digits[l_Index]);
        }

        if (options.HasFlag(Base32FormattingOptions.Pad))
        {
            int l_Padding = 8 - l_Builder.Length % 8;
            if (l_Padding > 0)
                l_Builder.Append('=', l_Padding == 8 ? 0 : l_Padding);
        }

        return l_Builder.ToString();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Convert a Base32 symbol to integer
    /// </summary>
    /// <param name="symbol">Input symbol</param>
    /// <returns>Symbol index or -1</returns>
    private static int SymbolToInt(char symbol)
    {
        switch (symbol)
        {
            case 'A': return 0;
            case 'B': return 1;
            case 'C': return 2;
            case 'D': return 3;
            case 'E': return 4;
            case 'F': return 5;
            case 'G': return 6;
            case 'H': return 7;
            case 'I': return 8;
            case 'J': return 9;
            case 'K': return 10;
            case 'L': return 11;
            case 'M': return 12;
            case 'N': return 13;
            case 'O': return 14;
            case 'P': return 15;
            case 'Q': return 16;
            case 'R': return 17;
            case 'S': return 18;
            case 'T': return 19;
            case 'U': return 20;
            case 'V': return 21;
            case 'W': return 22;
            case 'X': return 23;
            case 'Y': return 24;
            case 'Z': return 25;
            case '2': return 26;
            case '3': return 27;
            case '4': return 28;
            case '5': return 29;
            case '6': return 30;
            case '7': return 31;
        }

        return -1;
    }
}
