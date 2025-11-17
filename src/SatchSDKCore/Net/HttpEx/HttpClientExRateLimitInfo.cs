using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SSC.Net.HttpEx;

/// <summary>
/// Rate Limit Info
/// </summary>
public sealed class HttpClientExRateLimitInfo
{
    /// <summary>
    /// Total allowed requests for a given time window
    /// </summary>
    public int Limit { get; private set; }
    /// <summary>
    /// Number of requests remaining
    /// </summary>
    public int Remaining { get; private set; }
    /// <summary>
    /// Time at which rate limit window resets
    /// </summary>
    public DateTime Reset { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get HttpClientExRateLimitInfo from HttpResponseMessage
    /// </summary>
    /// <param name="coreHttpResponseMessage">Response</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static HttpClientExRateLimitInfo Get(HttpResponseMessage coreHttpResponseMessage)
    {
        if (coreHttpResponseMessage == null)
            throw new ArgumentNullException(nameof(coreHttpResponseMessage));

        var headers = GetFlattenedHeaders(coreHttpResponseMessage);

        return new HttpClientExRateLimitInfo()
        {
            Limit       = GetLimit(headers),
            Remaining   = GetRemaining(headers),
            Reset       = GetReset(headers),
        };
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get flattened headers from a HttpResponseMessage
    /// </summary>
    /// <param name="coreHttpResponseMessage">Response</param>
    /// <returns></returns>
    private static Dictionary<string, string> GetFlattenedHeaders(HttpResponseMessage coreHttpResponseMessage)
    {
        var result = new Dictionary<string, string>();

        foreach (var kvp in coreHttpResponseMessage.Headers)
        {
            var value = kvp.Value.FirstOrDefault(string.Empty);
            if (value.Contains(','))
            {
                var parts = value.Split(',');
                if (parts.Length > 0)
                    result.Add(kvp.Key, parts[0].Trim());
            }
            else
                result.Add(kvp.Key, value.Trim());
        }

        return result;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get limit value from header
    /// </summary>
    /// <param name="transformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static int GetLimit(IReadOnlyDictionary<string, string> transformedHeaders)
    {
        foreach (var kvp in transformedHeaders)
        {
            var keyLower = kvp.Key.ToLower();
            if (   keyLower == "x-rate-limit-limit" || keyLower == "x-ratelimit-limit"
                || keyLower == "rate-limit-limit"   || keyLower == "ratelimit-limit"
                || keyLower == "x-rate-limit-total" || keyLower == "x-ratelimit-total"
                || keyLower == "rate-limit-total"   || keyLower == "ratelimit-total")
            {
                if (int.TryParse(kvp.Value, out var value))
                    return value;
                else
                    return -1;
            }
        }

        return -1;
    }
    /// <summary>
    /// Get remaining value from header
    /// </summary>
    /// <param name="transformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static int GetRemaining(IReadOnlyDictionary<string, string> transformedHeaders)
    {
        foreach (var kvp in transformedHeaders)
        {
            var keyLower = kvp.Key.ToLower();
            if (   keyLower == "x-rate-limit-remaining" || keyLower == "x-ratelimit-remaining"
                || keyLower == "rate-limit-remaining"   || keyLower == "ratelimit-remaining")
            {
                if (int.TryParse(kvp.Value, out var value))
                    return value;
                else
                    return -1;
            }
        }

        return -1;
    }
    /// <summary>
    /// Get reset time from header
    /// </summary>
    /// <param name="transformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static DateTime GetReset(IReadOnlyDictionary<string, string> transformedHeaders)
    {
        foreach (var kvp in transformedHeaders)
        {
            var keyLower = kvp.Key.ToLower();
            if (   keyLower == "x-rate-limit-reset" || keyLower == "x-ratelimit-reset"
                || keyLower == "rate-limit-reset"   || keyLower == "ratelimit-reset")
            {
                if (!long.TryParse(kvp.Value, out var value))
                    return DateTime.Now.AddSeconds(2);

                if (value < 1000000000)
                    return Misc.Time.FromUnixTime(Misc.Time.UnixTimeNow() + value);

                return Misc.Time.FromUnixTime(value);
            }
        }

        return DateTime.Now.AddSeconds(2);
    }
}
