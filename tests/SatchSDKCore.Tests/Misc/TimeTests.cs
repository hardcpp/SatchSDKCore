using System;
using System.Globalization;
using SSC.Misc;

namespace SatchSDKCore.Tests.Misc;

/// <summary>
/// Tests for Time utility class which provides Unix timestamp conversion,
/// date/time parsing, and month name constants.
/// </summary>
public class TimeTests
{
    /// <summary>
    /// Verifies that MonthNames property returns an array of all 12 month names in English.
    /// </summary>
    [Fact]
    public void MonthNames_ReturnsAllTwelveMonths()
    {
        // Act
        var months = Time.MonthNames;

        // Assert
        Assert.NotNull(months);
        Assert.Equal(12, months.Length);
        Assert.Equal("January", months[0]);
        Assert.Equal("December", months[11]);
    }

    /// <summary>
    /// Verifies that MonthNamesShort property returns an array of all 12 abbreviated month names.
    /// </summary>
    [Fact]
    public void MonthNamesShort_ReturnsAllTwelveMonthsShort()
    {
        // Act
        var months = Time.MonthNamesShort;

        // Assert
        Assert.NotNull(months);
        Assert.Equal(12, months.Length);
        Assert.Equal("Jan.", months[0]);
        Assert.Equal("Dec.", months[11]);
    }

    /// <summary>
    /// Verifies that MonthNames array contains all expected month names from January to December.
    /// </summary>
    [Fact]
    public void MonthNames_ContainsExpectedMonths()
    {
        // Act
        var months = Time.MonthNames;

        // Assert
        Assert.Contains("January", months);
        Assert.Contains("February", months);
        Assert.Contains("March", months);
        Assert.Contains("April", months);
        Assert.Contains("May", months);
        Assert.Contains("June", months);
        Assert.Contains("July", months);
        Assert.Contains("August", months);
        Assert.Contains("September", months);
        Assert.Contains("October", months);
        Assert.Contains("November", months);
        Assert.Contains("December", months);
    }

    /// <summary>
    /// Verifies that UnixTimeNow() returns the current time as a positive Unix timestamp in seconds.
    /// </summary>
    [Fact]
    public void UnixTimeNow_ReturnsPositiveValue()
    {
        // Act
        var unixTime = Time.UnixTimeNow();

        // Assert
        Assert.True(unixTime > 0);
        // Should be after 2020-01-01 (1577836800)
        Assert.True(unixTime > 1577836800);
    }

    /// <summary>
    /// Verifies that UnixTimeNowMS() returns the current time as a positive Unix timestamp in milliseconds.
    /// </summary>
    [Fact]
    public void UnixTimeNowMS_ReturnsPositiveValue()
    {
        // Act
        var unixTimeMs = Time.UnixTimeNowMS();

        // Assert
        Assert.True(unixTimeMs > 0);
        // Should be after 2020-01-01 in milliseconds
        Assert.True(unixTimeMs > 1577836800000);
    }

    /// <summary>
    /// Verifies that UnixTimeNowMS() returns a value approximately 1000x larger than UnixTimeNow()
    /// since it includes milliseconds.
    /// </summary>
    [Fact]
    public void UnixTimeNowMS_IsLargerThanUnixTimeNow()
    {
        // Act
        var unixTime = Time.UnixTimeNow();
        var unixTimeMs = Time.UnixTimeNowMS();

        // Assert
        // Milliseconds should be roughly 1000x larger
        Assert.True(unixTimeMs > unixTime * 900); // Allow some margin
    }

    /// <summary>
    /// Verifies that ToUnixTime() correctly converts a known DateTime to its Unix timestamp.
    /// </summary>
    [Fact]
    public void ToUnixTime_KnownDateTime_ReturnsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedUnixTime = 1577836800L;

        // Act
        var unixTime = Time.ToUnixTime(dateTime);

        // Assert
        Assert.Equal(expectedUnixTime, unixTime);
    }

    /// <summary>
    /// Verifies that ToUnixTime() returns 0 for the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
    [Fact]
    public void ToUnixTime_UnixEpoch_ReturnsZero()
    {
        // Arrange
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var unixTime = Time.ToUnixTime(dateTime);

        // Assert
        Assert.Equal(0L, unixTime);
    }

    /// <summary>
    /// Verifies that ToUnixTimeMS() correctly converts a known DateTime to its Unix timestamp in milliseconds.
    /// </summary>
    [Fact]
    public void ToUnixTimeMS_KnownDateTime_ReturnsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedUnixTimeMs = 1577836800000L;

        // Act
        var unixTimeMs = Time.ToUnixTimeMS(dateTime);

        // Assert
        Assert.Equal(expectedUnixTimeMs, unixTimeMs);
    }

    /// <summary>
    /// Verifies that ToUnixTimeMS() returns 0 for the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
    [Fact]
    public void ToUnixTimeMS_UnixEpoch_ReturnsZero()
    {
        // Arrange
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var unixTimeMs = Time.ToUnixTimeMS(dateTime);

        // Assert
        Assert.Equal(0L, unixTimeMs);
    }

    /// <summary>
    /// Verifies that ToUnixTime() automatically converts local DateTime to UTC before conversion,
    /// ensuring consistent results regardless of DateTimeKind.
    /// </summary>
    [Fact]
    public void ToUnixTime_LocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var utcDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var localDateTime = utcDateTime.ToLocalTime();

        // Act
        var unixTimeFromUtc = Time.ToUnixTime(utcDateTime);
        var unixTimeFromLocal = Time.ToUnixTime(localDateTime);

        // Assert
        Assert.Equal(unixTimeFromUtc, unixTimeFromLocal);
    }

    /// <summary>
    /// Verifies that FromUnixTime() returns the Unix epoch when given a timestamp of 0.
    /// </summary>
    [Fact]
    public void FromUnixTime_Zero_ReturnsUnixEpoch()
    {
        // Arrange
        var unixTime = 0L;

        // Act
        var dateTime = Time.FromUnixTime(unixTime);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(1970, utcDateTime.Year);
        Assert.Equal(1, utcDateTime.Month);
        Assert.Equal(1, utcDateTime.Day);
        Assert.Equal(0, utcDateTime.Hour);
        Assert.Equal(0, utcDateTime.Minute);
        Assert.Equal(0, utcDateTime.Second);
    }

    /// <summary>
    /// Verifies that FromUnixTime() correctly converts a known Unix timestamp to DateTime.
    /// </summary>
    [Fact]
    public void FromUnixTime_KnownTimestamp_ReturnsCorrectDateTime()
    {
        // Arrange
        var unixTime = 1577836800L; // 2020-01-01 00:00:00 UTC

        // Act
        var dateTime = Time.FromUnixTime(unixTime);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(2020, utcDateTime.Year);
        Assert.Equal(1, utcDateTime.Month);
        Assert.Equal(1, utcDateTime.Day);
    }

    /// <summary>
    /// Verifies that FromUnixTimeMS() returns the Unix epoch when given a timestamp of 0.
    /// </summary>
    [Fact]
    public void FromUnixTimeMS_Zero_ReturnsUnixEpoch()
    {
        // Arrange
        var unixTimeMs = 0L;

        // Act
        var dateTime = Time.FromUnixTimeMS(unixTimeMs);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(1970, utcDateTime.Year);
        Assert.Equal(1, utcDateTime.Month);
        Assert.Equal(1, utcDateTime.Day);
        Assert.Equal(0, utcDateTime.Hour);
        Assert.Equal(0, utcDateTime.Minute);
        Assert.Equal(0, utcDateTime.Second);
    }

    /// <summary>
    /// Verifies that FromUnixTimeMS() correctly converts a known Unix timestamp in milliseconds to DateTime.
    /// </summary>
    [Fact]
    public void FromUnixTimeMS_KnownTimestamp_ReturnsCorrectDateTime()
    {
        // Arrange
        var unixTimeMs = 1577836800000L; // 2020-01-01 00:00:00 UTC

        // Act
        var dateTime = Time.FromUnixTimeMS(unixTimeMs);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(2020, utcDateTime.Year);
        Assert.Equal(1, utcDateTime.Month);
        Assert.Equal(1, utcDateTime.Day);
    }

    /// <summary>
    /// Verifies that FromUnixTimeMS() preserves millisecond precision when converting from Unix timestamp.
    /// </summary>
    [Fact]
    public void FromUnixTimeMS_WithMilliseconds_PreservesMilliseconds()
    {
        // Arrange
        var unixTimeMs = 1577836800500L; // 2020-01-01 00:00:00.500 UTC

        // Act
        var dateTime = Time.FromUnixTimeMS(unixTimeMs);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(500, utcDateTime.Millisecond);
    }

    /// <summary>
    /// Verifies that converting DateTime to Unix timestamp and back preserves the date and time
    /// (excluding milliseconds which are lost in second-precision conversion).
    /// </summary>
    [Fact]
    public void RoundTrip_ToUnixTimeAndBack_PreservesDateTime()
    {
        // Arrange
        var originalDateTime = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);

        // Act
        var unixTime = Time.ToUnixTime(originalDateTime);
        var roundTripDateTime = Time.FromUnixTime(unixTime);

        // Assert
        var utcRoundTrip = roundTripDateTime.ToUniversalTime();
        Assert.Equal(originalDateTime.Year, utcRoundTrip.Year);
        Assert.Equal(originalDateTime.Month, utcRoundTrip.Month);
        Assert.Equal(originalDateTime.Day, utcRoundTrip.Day);
        Assert.Equal(originalDateTime.Hour, utcRoundTrip.Hour);
        Assert.Equal(originalDateTime.Minute, utcRoundTrip.Minute);
        Assert.Equal(originalDateTime.Second, utcRoundTrip.Second);
    }

    /// <summary>
    /// Verifies that converting DateTime to Unix timestamp in milliseconds and back preserves
    /// the complete date, time, and millisecond precision.
    /// </summary>
    [Fact]
    public void RoundTrip_ToUnixTimeMSAndBack_PreservesDateTime()
    {
        // Arrange
        var originalDateTime = new DateTime(2023, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc);

        // Act
        var unixTimeMs = Time.ToUnixTimeMS(originalDateTime);
        var roundTripDateTime = Time.FromUnixTimeMS(unixTimeMs);

        // Assert
        var utcRoundTrip = roundTripDateTime.ToUniversalTime();
        Assert.Equal(originalDateTime.Year, utcRoundTrip.Year);
        Assert.Equal(originalDateTime.Month, utcRoundTrip.Month);
        Assert.Equal(originalDateTime.Day, utcRoundTrip.Day);
        Assert.Equal(originalDateTime.Hour, utcRoundTrip.Hour);
        Assert.Equal(originalDateTime.Minute, utcRoundTrip.Minute);
        Assert.Equal(originalDateTime.Second, utcRoundTrip.Second);
        Assert.Equal(originalDateTime.Millisecond, utcRoundTrip.Millisecond);
    }

    /// <summary>
    /// Verifies that TryParseInternational() successfully parses a valid ISO 8601 datetime string.
    /// </summary>
    [Fact]
    public void TryParseInternational_ValidIso8601_ReturnsTrue()
    {
        // Arrange
        var input = "2023-06-15T12:30:45Z";

        // Act
        var result = Time.TryParseInternational(input, out var dateTime);

        // Assert
        Assert.True(result);
        Assert.Equal(2023, dateTime.Year);
        Assert.Equal(6, dateTime.Month);
        Assert.Equal(15, dateTime.Day);
        Assert.Equal(12, dateTime.Hour);
        Assert.Equal(30, dateTime.Minute);
        Assert.Equal(45, dateTime.Second);
    }

    /// <summary>
    /// Verifies that TryParseInternational() successfully parses a valid date-only string.
    /// </summary>
    [Fact]
    public void TryParseInternational_ValidDate_ReturnsTrue()
    {
        // Arrange
        var input = "2023-06-15";

        // Act
        var result = Time.TryParseInternational(input, out var dateTime);

        // Assert
        Assert.True(result);
        Assert.Equal(2023, dateTime.Year);
        Assert.Equal(6, dateTime.Month);
        Assert.Equal(15, dateTime.Day);
    }

    /// <summary>
    /// Verifies that TryParseInternational() returns false for invalid date strings
    /// and sets the output to default DateTime.
    /// </summary>
    [Fact]
    public void TryParseInternational_InvalidDate_ReturnsFalse()
    {
        // Arrange
        var input = "not-a-date";

        // Act
        var result = Time.TryParseInternational(input, out var dateTime);

        // Assert
        Assert.False(result);
        Assert.Equal(default(DateTime), dateTime);
    }

    /// <summary>
    /// Verifies that TryParseInternational() returns false for empty strings.
    /// </summary>
    [Fact]
    public void TryParseInternational_EmptyString_ReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = Time.TryParseInternational(input, out var dateTime);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that TryParseInternational() correctly parses datetime strings with timezone offsets.
    /// </summary>
    [Fact]
    public void TryParseInternational_WithTimeZone_ParsesCorrectly()
    {
        // Arrange
        var input = "2023-06-15T12:30:45+02:00";

        // Act
        var result = Time.TryParseInternational(input, out var dateTime);

        // Assert
        Assert.True(result);
        Assert.Equal(2023, dateTime.Year);
        Assert.Equal(6, dateTime.Month);
        Assert.Equal(15, dateTime.Day);
    }

    /// <summary>
    /// Verifies that TryParseInternational() uses InvariantCulture for parsing,
    /// ensuring consistent behavior regardless of the current culture settings.
    /// </summary>
    [Fact]
    public void TryParseInternational_UsesInvariantCulture()
    {
        // Arrange
        var input = "2023-06-15T12:30:45";
        var currentCulture = CultureInfo.CurrentCulture;

        try
        {
            // Change culture to test invariant parsing
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            var result = Time.TryParseInternational(input, out var dateTime);

            // Assert
            Assert.True(result);
            Assert.Equal(2023, dateTime.Year);
            Assert.Equal(6, dateTime.Month);
            Assert.Equal(15, dateTime.Day);
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
        }
    }

    /// <summary>
    /// Verifies that ToUnixTime() returns a large positive value for future dates (after 2033).
    /// </summary>
    [Fact]
    public void ToUnixTime_FutureDate_ReturnsLargePositiveValue()
    {
        // Arrange
        var futureDate = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var unixTime = Time.ToUnixTime(futureDate);

        // Assert
        Assert.True(unixTime > 2000000000L); // After 2033
    }

    /// <summary>
    /// Verifies that ToUnixTime() returns a small positive value for past dates (before 2001).
    /// </summary>
    [Fact]
    public void ToUnixTime_PastDate_ReturnsSmallPositiveValue()
    {
        // Arrange
        var pastDate = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var unixTime = Time.ToUnixTime(pastDate);

        // Assert
        Assert.True(unixTime > 0);
        Assert.True(unixTime < 1000000000L); // Before 2001
    }

    /// <summary>
    /// Verifies that FromUnixTime() correctly handles negative timestamps,
    /// returning dates before the Unix epoch (before January 1, 1970).
    /// </summary>
    [Fact]
    public void FromUnixTime_NegativeValue_ReturnsDateBeforeEpoch()
    {
        // Arrange
        var unixTime = -86400L; // One day before epoch

        // Act
        var dateTime = Time.FromUnixTime(unixTime);

        // Assert
        var utcDateTime = dateTime.ToUniversalTime();
        Assert.Equal(1969, utcDateTime.Year);
        Assert.Equal(12, utcDateTime.Month);
        Assert.Equal(31, utcDateTime.Day);
    }
}
