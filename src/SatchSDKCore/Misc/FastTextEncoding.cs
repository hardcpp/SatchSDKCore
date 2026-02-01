using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SSC.Misc;

public static class FastTextEncoding
{
    /// <summary>
    /// Returns the number of bytes required to encode a the characters in a string
    /// </summary>
    /// <param name="str">Source string</param>
    /// <param name="encoding">Target encoding</param>
    /// <returns>Number of bytes required to encode a the characters in a string</returns>
    /// <exception cref="NotSupportedException">If the encoding is not supported</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetByteCount(ReadOnlySpan<char> str, Encoding encoding)
    {
        if (encoding is UTF8Encoding utf8Encoding)
        {
            unsafe
            {
                fixed (char* chars = &MemoryMarshal.GetReference(str))
                    return utf8Encoding!.GetByteCount(chars, str.Length);
            }
        }

        if (encoding is UTF32Encoding utf32Encoding)
        {
            unsafe
            {
                fixed (char* chars = &MemoryMarshal.GetReference(str))
                    return utf32Encoding!.GetByteCount(chars, str.Length);
            }
        }

        throw new NotSupportedException($"Encoding {encoding.EncodingName} is not supported.");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Fill a byte array containing the encoded representation of the given
    /// </summary>
    /// <param name="str">Source string</param>
    /// <param name="encoding">Target encoding</param>
    /// <param name="bytes">Destination byte array</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="NotSupportedException">If the encoding is not supported</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytes(ReadOnlySpan<char> str, Encoding encoding, Span<byte> bytes)
    {
        if (encoding is UTF8Encoding utf8Encoding)
        {
            unsafe
            {
                fixed (char* chars = &MemoryMarshal.GetReference(str))
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                    return utf8Encoding!.GetBytes(chars, str.Length, bytesPtr, bytes.Length);
            }
        }

        if (encoding is UTF32Encoding utf32Encoding)
        {
            unsafe
            {
                fixed (char* chars = &MemoryMarshal.GetReference(str))
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                    return utf32Encoding!.GetBytes(chars, str.Length, bytesPtr, bytes.Length);
            }
        }

        throw new NotSupportedException($"Encoding {encoding.EncodingName} is not supported.");
    }
}
