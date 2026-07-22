using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace SSC.Misc;

[Flags]
public enum EBase32FormattingOptions
{
    None = 0,
    Pad  = 1
}

/// <summary>
/// Base32 encoding/decoding utilities
/// </summary>
public static class Base32
{
    private const           int    BIT_MASK  = 31;
    private const           int    BIT_SHIFT = 5;
    private static readonly char[] s_Digits  = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

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
    /// Converts the specified range of a Char array, which encodes binary data as Base32 digits, to the equivalent byte
    /// array.
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

        int    outLength     = charSpan.Length * BIT_SHIFT / 8;
        byte[] result        = new byte[outLength];
        int    bitsBuffer    = 0;
        int    bitsRemaining = 0;
        int    next          = 0;
        int    symboNumber   = 0;

        for (int i = 0; i < charSpan.Length; ++i)
        {
            symboNumber = SymbolToInt(char.ToUpper(charSpan[i]));
            if (symboNumber < 0)
                throw new FormatException("Illegal character: `" + charSpan[i] + "`");

            bitsBuffer    <<= BIT_SHIFT;
            bitsBuffer    |=  symboNumber & BIT_MASK;
            bitsRemaining +=  BIT_SHIFT;

            if (bitsRemaining >= 8)
            {
                result[next++] =  (byte)(bitsBuffer >> (bitsRemaining - 8));
                bitsRemaining  -= 8;
            }
        }

        return result;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-32
    /// digits
    /// </summary>
    /// <param name="inArray">An array of 8-bit unsigned integers</param>
    /// <param name="options">Encoding options</param>
    /// <returns>The string representation in base 32 of the elements in <paramref name="inArray" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32String(
        byte[]                   inArray,
        EBase32FormattingOptions options = EBase32FormattingOptions.None)
    {
        ArgumentNullException.ThrowIfNull(inArray);

        return ToBase32String(new ReadOnlySpan<byte>(inArray), options);
    }

    /// <summary>
    /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded
    /// with base-32 digits.
    /// Parameters specify the subset as an offset in the input array, and the number of elements in the array to convert
    /// </summary>
    /// <param name="inArray">An array of 8-bit unsigned integers</param>
    /// <param name="offset">An offset in <paramref name="inArray" /></param>
    /// <param name="length">The number of elements of <paramref name="inArray" /> to convert</param>
    /// <param name="options">Encoding options</param>
    /// <returns>
    /// The string representation in base 32 of <paramref name="length" /> elements of <paramref name="inArray" />,
    /// starting at position <paramref name="offset" />
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32String(
        byte[]                   inArray,
        int                      offset,
        int                      length,
        EBase32FormattingOptions options = EBase32FormattingOptions.None)
    {
        ArgumentNullException.ThrowIfNull(inArray);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, inArray.Length - length);

        return ToBase32String(new ReadOnlySpan<byte>(inArray, offset, length), options);
    }

    /// <summary>
    /// Converts a subset of an array of 8-bit unsigned integers to its equivalent string representation that is encoded
    /// with base-32 digits.
    /// </summary>
    /// <param name="bytes">A read-only span of 8-bit unsigned integers</param>
    /// <param name="options">Encoding options</param>
    /// <returns>
    /// The string representation in base 32 of the elements in bytes. If the length of bytes is 0, an empty string is
    /// returned
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If the input bytes are too big</exception>
    public static string ToBase32String(
        ReadOnlySpan<byte>       bytes,
        EBase32FormattingOptions options = EBase32FormattingOptions.None)
    {
        if (bytes.Length == 0)
            return string.Empty;

        if (bytes.Length >= 1 << 28)
            throw new ArgumentOutOfRangeException(nameof(bytes));

        int outputLength  = ((bytes.Length * 8) + BIT_SHIFT - 1) / BIT_SHIFT;
        var builder       = new StringBuilder(outputLength);
        int position      = 0;
        int lastPosition  = position + bytes.Length;
        int bitsBuffer    = bytes[position++];
        int bitsRemaining = 8;

        while (bitsRemaining > 0 || position < lastPosition)
        {
            if (bitsRemaining < BIT_SHIFT)
            {
                if (position < lastPosition)
                {
                    bitsBuffer    <<= 8;
                    bitsBuffer    |=  bytes[position++] & 0xFF;
                    bitsRemaining +=  8;
                }
                else
                {
                    int padding = BIT_SHIFT - bitsRemaining;
                    bitsBuffer    <<= padding;
                    bitsRemaining +=  padding;
                }
            }

            int index = BIT_MASK & (bitsBuffer >> (bitsRemaining - BIT_SHIFT));
            bitsRemaining -= BIT_SHIFT;

            builder.Append(s_Digits[index]);
        }

        if (options.HasFlag(EBase32FormattingOptions.Pad))
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
        => symbol switch
        {
            'A' => 0,
            'B' => 1,
            'C' => 2,
            'D' => 3,
            'E' => 4,
            'F' => 5,
            'G' => 6,
            'H' => 7,
            'I' => 8,
            'J' => 9,
            'K' => 10,
            'L' => 11,
            'M' => 12,
            'N' => 13,
            'O' => 14,
            'P' => 15,
            'Q' => 16,
            'R' => 17,
            'S' => 18,
            'T' => 19,
            'U' => 20,
            'V' => 21,
            'W' => 22,
            'X' => 23,
            'Y' => 24,
            'Z' => 25,
            '2' => 26,
            '3' => 27,
            '4' => 28,
            '5' => 29,
            '6' => 30,
            '7' => 31,
            _   => -1
        };
}
