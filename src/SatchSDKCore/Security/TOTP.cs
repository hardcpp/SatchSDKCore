using SSC.Misc;
using System;
using System.Security.Cryptography;
using System.Web;

namespace SSC.Security;

/// <summary>
/// Time based One Time Password (Sha1)
/// </summary>
public class TOTP
{
    private const long c_UnixEpochTicks = 621355968000000000L;
    private const long c_TicksToSeconds = 10000000L;

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
        var l_Window = CalculateTimeStepFromTimestamp(DateTime.UtcNow, period) + windowOffset;
        var l_Data = GetBigEndianBytes(l_Window);

        var l_HMAC = new HMACSHA1();
        l_HMAC.Key = secret;
        var l_HMACComputedHash = l_HMAC.ComputeHash(l_Data);

        var l_Offset = l_HMACComputedHash[l_HMACComputedHash.Length - 1] & 0x0F;
        var l_OTP = (l_HMACComputedHash[l_Offset + 0] & 0x7F) << 24
                     | (l_HMACComputedHash[l_Offset + 1] & 0xFF) << 16
                     | (l_HMACComputedHash[l_Offset + 2] & 0xFF) << 8
                     | (l_HMACComputedHash[l_Offset + 3] & 0xFF);

        return OTPToDigits(l_OTP, digits);
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
        var l_UnixTimestamp = (dateTime.Ticks - c_UnixEpochTicks) / c_TicksToSeconds;
        var l_Window = l_UnixTimestamp / (long)period;
        return l_Window;
    }
    /// <summary>
    /// Convert OTP to digits
    /// </summary>
    /// <param name="otp">Input OTP</param>
    /// <param name="digits">Numbers of digits</param>
    /// <returns></returns>
    private static string OTPToDigits(long otp, int digits)
    {
        var l_TruncatedValue = ((int)otp % (int)Math.Pow(10, digits));
        return l_TruncatedValue.ToString().PadLeft(digits, '0');
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
        var l_Data = BitConverter.GetBytes(input);
        Array.Reverse(l_Data);
        return l_Data;
    }
}