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
        if (encoding is UTF8Encoding l_UTF8Encoding)
        {
            unsafe
            {
                fixed (char* l_Chars = &MemoryMarshal.GetReference(str))
                    return l_UTF8Encoding!.GetByteCount(l_Chars, str.Length);
            }
        }
        else if (encoding is UTF32Encoding l_UTF32Encoding)
        {
            unsafe
            {
                fixed (char* l_Chars = &MemoryMarshal.GetReference(str))
                    return l_UTF32Encoding!.GetByteCount(l_Chars, str.Length);
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
        if (encoding is UTF8Encoding l_UTF8Encoding)
        {
            unsafe
            {
                fixed (char* l_Chars = &MemoryMarshal.GetReference(str))
                fixed (byte* l_BytesPtr = &MemoryMarshal.GetReference(bytes))
                    return l_UTF8Encoding!.GetBytes(l_Chars, str.Length, l_BytesPtr, bytes.Length);
            }
        }
        else if (encoding is UTF32Encoding l_UTF32Encoding)
        {
            unsafe
            {
                fixed (char* l_Chars = &MemoryMarshal.GetReference(str))
                fixed (byte* l_BytesPtr = &MemoryMarshal.GetReference(bytes))
                    return l_UTF32Encoding!.GetBytes(l_Chars, str.Length, l_BytesPtr, bytes.Length);
            }
        }

        throw new NotSupportedException($"Encoding {encoding.EncodingName} is not supported.");
    }
}
