using System;
using System.Globalization;

namespace SSC.Misc
{
    /// <summary>
    /// Time helper
    /// </summary>
    public static class Time
    {
        private static readonly DateTime s_UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static string[] s_Months = [
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
        private static string[] s_MonthsShort = [
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

        public static string[] MonthNames => s_Months;
        public static string[] MonthNamesShort => s_MonthsShort;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get UnixTimestamp
        /// </summary>
        /// <returns>Unix timestamp</returns>
        public static Int64 UnixTimeNow()
            => (Int64)(DateTime.UtcNow - s_UnixEpoch).TotalSeconds;
        /// <summary>
        /// Get UnixTimestamp
        /// </summary>
        /// <returns>Unix timestamp</returns>
        public static Int64 UnixTimeNowMS()
            => (Int64)(DateTime.UtcNow - s_UnixEpoch).TotalMilliseconds;
        /// <summary>
        /// Convert DateTime to UnixTimestamp
        /// </summary>
        /// <param name="dateTime">The DateTime to convert</param>
        /// <returns></returns>
        public static Int64 ToUnixTime(DateTime dateTime)
            => (Int64)dateTime.ToUniversalTime().Subtract(s_UnixEpoch).TotalSeconds;
        /// <summary>
        /// Convert DateTime to UnixTimestamp
        /// </summary>
        /// <param name="dateTime">The DateTime to convert</param>
        /// <returns></returns>
        public static Int64 ToUnixTimeMS(DateTime dateTime)
            => (Int64)dateTime.ToUniversalTime().Subtract(s_UnixEpoch).TotalMilliseconds;
        /// <summary>
        /// Convert UnixTimestamp to DateTime
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime FromUnixTime(Int64 timestamp)
            => s_UnixEpoch.AddSeconds(timestamp).ToLocalTime();
        /// <summary>
        /// Convert UnixTimestamp to DateTime
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime FromUnixTimeMS(Int64 timestamp)
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
}
