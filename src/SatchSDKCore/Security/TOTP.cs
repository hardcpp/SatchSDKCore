using System;
using System.Security.Cryptography;
using System.Web;
using SSC.Misc;

namespace SSC.Security;

/// <summary>
/// Time based One Time Password (Sha1)
/// </summary>
public class TOTP
{
    private const long UNIX_EPOCH_TICKS = 621355968000000000L;
    private const long TICKS_TO_SECONDS = 10000000L;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Compute a TOTP code
    /// </summary>
    /// <param name="secret">Shared secret</param>
    /// <param name="digits">Numbers of digits</param>
    /// <param name="period">Period</param>
    /// <returns></returns>
    public static string ComputeCode(byte[] secret, int digits = 6, int period = 30, int windowOffset = 0)
    {
        var window = CalculateTimeStepFromTimestamp(DateTime.UtcNow, period) + windowOffset;
        var data = GetBigEndianBytes(window);

        var hmacSha1 = new HMACSHA1();
        hmacSha1.Key = secret;
        var hMACComputedHash = hmacSha1.ComputeHash(data);
        hmacSha1.Dispose();

        var offset = hMACComputedHash[hMACComputedHash.Length - 1] & 0x0F;
        var otp = ((hMACComputedHash[offset + 0] & 0x7F) << 24)
                     | ((hMACComputedHash[offset + 1] & 0xFF) << 16)
                     | ((hMACComputedHash[offset + 2] & 0xFF) << 8)
                     | (hMACComputedHash[offset + 3] & 0xFF);

        return OTPToDigits(otp, digits);
    }
    /// <summary>
    /// Forge an OTPAuth url
    /// </summary>
    /// <param name="label">Label</param>
    /// <param name="issuer">Who isued the TOTP</param>
    /// <param name="secret">Shared secret</param>
    /// <param name="digits">Numbers of digits</param>
    /// <param name="period">Period</param>
    /// <returns></returns>
    public static string ForgeURL(string label, string issuer, byte[] secret, int digits = 6, int period = 30)
    {
        return $"otpauth://totp/{HttpUtility.UrlEncode(label)}" +
            $"?secret={Base32.ToBase32String(secret)}" +
            $"&digits={digits}" +
            $"&period={period}" +
            $"&issuer={HttpUtility.UrlEncode(issuer)}";
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get time step from timestamp
    /// </summary>
    /// <param name="dateTime">Timestamp</param>
    /// <param name="period">Period</param>
    /// <returns></returns>
    private static long CalculateTimeStepFromTimestamp(DateTime dateTime, int period)
    {
        var unixTimestamp = (dateTime.Ticks - UNIX_EPOCH_TICKS) / TICKS_TO_SECONDS;
        var window = unixTimestamp / period;
        return window;
    }
    /// <summary>
    /// Convert OTP to digits
    /// </summary>
    /// <param name="otp">Input OTP</param>
    /// <param name="digits">Numbers of digits</param>
    /// <returns></returns>
    private static string OTPToDigits(long otp, int digits)
    {
        var truncatedValue = ((int)otp % (int)Math.Pow(10, digits));
        return truncatedValue.ToString().PadLeft(digits, '0');
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get big endian bytes
    /// </summary>
    /// <param name="input">Input data</param>
    /// <returns></returns>
    private static byte[] GetBigEndianBytes(long input)
    {
        var data = BitConverter.GetBytes(input);
        Array.Reverse(data);
        return data;
    }
}
