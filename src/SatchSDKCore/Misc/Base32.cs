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

        int outLength = charSpan.Length * c_Shift / 8;
        var result = new byte[outLength];
        int bitsBuffer = 0;
        int bitsRemaining = 0;
        int next = 0;
        int symboNumber = 0;

        for (var i = 0; i < charSpan.Length; ++i)
        {
            symboNumber = SymbolToInt(char.ToUpper(charSpan[i]));
            if (symboNumber < 0)
                throw new FormatException("Illegal character: `" + charSpan[i] + "`");

            bitsBuffer <<= c_Shift;
            bitsBuffer |= symboNumber & c_Mask;
            bitsRemaining += c_Shift;

            if (bitsRemaining >= 8)
            {
                result[next++] = (byte)(bitsBuffer >> (bitsRemaining - 8));
                bitsRemaining -= 8;
            }
        }

        return result;
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

        int outputLength = ((bytes.Length * 8) + c_Shift - 1) / c_Shift;
        var builder = new StringBuilder(outputLength);
        int position = 0;
        int lastPosition = position + bytes.Length;
        int bitsBuffer = bytes[position++];
        int bitsRemaining = 8;

        while (bitsRemaining > 0 || position < lastPosition)
        {
            if (bitsRemaining < c_Shift)
            {
                if (position < lastPosition)
                {
                    bitsBuffer <<= 8;
                    bitsBuffer |= bytes[position++] & 0xFF;
                    bitsRemaining += 8;
                }
                else
                {
                    int padding = c_Shift - bitsRemaining;
                    bitsBuffer <<= padding;
                    bitsRemaining += padding;
                }
            }

            int index = c_Mask & (bitsBuffer >> (bitsRemaining - c_Shift));
            bitsRemaining -= c_Shift;

            builder.Append(m_Digits[index]);
        }

        if (options.HasFlag(Base32FormattingOptions.Pad))
        {
            int padding = 8 - (builder.Length % 8);
            if (padding > 0)
                builder.Append('=', padding == 8 ? 0 : padding);
        }

        return builder.ToString();
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
