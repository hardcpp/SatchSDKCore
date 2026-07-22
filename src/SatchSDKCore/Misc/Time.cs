using System;
using System.Globalization;

namespace SSC.Misc;

/// <summary>
/// Time helper
/// </summary>
public static class Time
{
    private static readonly DateTime s_UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public static string[] MonthNames { get; } =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];

    public static string[] MonthNamesShort { get; } =
    [
        "Jan.",
        "Feb.",
        "Mar.",
        "Apr.",
        "May",
        "Jun.",
        "Jul.",
        "Aug.",
        "Sept.",
        "Oct.",
        "Nov.",
        "Dec."
    ];

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get UnixTimestamp
    /// </summary>
    /// <returns>Unix timestamp</returns>
    public static long UnixTimeNow()
        => (long)(DateTime.UtcNow - s_UnixEpoch).TotalSeconds;

    /// <summary>
    /// Get UnixTimestamp
    /// </summary>
    /// <returns>Unix timestamp</returns>
    public static long UnixTimeNowMS()
        => (long)(DateTime.UtcNow - s_UnixEpoch).TotalMilliseconds;

    /// <summary>
    /// Convert DateTime to UnixTimestamp
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns></returns>
    public static long ToUnixTime(DateTime dateTime)
        => (long)dateTime.ToUniversalTime().Subtract(s_UnixEpoch).TotalSeconds;

    /// <summary>
    /// Convert DateTime to UnixTimestamp
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns></returns>
    public static long ToUnixTimeMS(DateTime dateTime)
        => (long)dateTime.ToUniversalTime().Subtract(s_UnixEpoch).TotalMilliseconds;

    /// <summary>
    /// Convert UnixTimestamp to DateTime
    /// </summary>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static DateTime FromUnixTime(long timestamp)
        => s_UnixEpoch.AddSeconds(timestamp).ToLocalTime();

    /// <summary>
    /// Convert UnixTimestamp to DateTime
    /// </summary>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static DateTime FromUnixTimeMS(long timestamp)
        => s_UnixEpoch.AddMilliseconds(timestamp).ToLocalTime();

    /// <summary>
    /// Try parse international data
    /// </summary>
    /// <param name="input"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParseInternational(string input, out DateTime result)
        => DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
}
