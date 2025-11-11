using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SSC.Net.HTTPClient;

/// <summary>
/// Rate Limit Info
/// </summary>
public sealed class HTTPClientRateLimitInfo
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
    /// Get HTTPClientRateLimitInfo from HttpResponseMessage
    /// </summary>
    /// <param name="coreHttpResponseMessage">Response</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static HTTPClientRateLimitInfo Get(HttpResponseMessage coreHttpResponseMessage)
    {
        if (coreHttpResponseMessage == null)
            throw new ArgumentNullException(nameof(coreHttpResponseMessage));

        var headers = GetTransformedHeaders(coreHttpResponseMessage);

        return new HTTPClientRateLimitInfo()
        {
            Limit       = GetLimit(headers),
            Remaining   = GetRemaining(headers),
            Reset       = GetReset(headers),
        };
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get transformed headers from a HttpResponseMessage
    /// </summary>
    /// <param name="coreHttpResponseMessage">Response</param>
    /// <returns></returns>
    private static Dictionary<string, string> GetTransformedHeaders(HttpResponseMessage coreHttpResponseMessage)
    {
        var l_Result = new Dictionary<string, string>();

        foreach (var l_KVP in coreHttpResponseMessage.Headers)
        {
            if (l_KVP.Value.FirstOrDefault().Contains(","))
                l_Result.Add(l_KVP.Key, l_KVP.Value.FirstOrDefault().Split(',').FirstOrDefault()?.Trim());
            else
                l_Result.Add(l_KVP.Key, l_KVP.Value.FirstOrDefault().Trim());
        }

        return l_Result;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get limit value from header
    /// </summary>
    /// <param name="p_TransformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static int GetLimit(Dictionary<string, string> p_TransformedHeaders)
    {
        foreach (var l_KVP in p_TransformedHeaders)
        {
            var l_Lower = l_KVP.Key.ToLower();
            if (   l_Lower == "x-rate-limit-limit" || l_Lower == "x-ratelimit-limit"
                || l_Lower == "rate-limit-limit"   || l_Lower == "ratelimit-limit"
                || l_Lower == "x-rate-limit-total" || l_Lower == "x-ratelimit-total"
                || l_Lower == "rate-limit-total"   || l_Lower == "ratelimit-total")
            {
                if (int.TryParse(l_KVP.Value, out var l_Value))
                    return l_Value;
                else
                    return -1;
            }
        }

        return -1;
    }
    /// <summary>
    /// Get remaining value from header
    /// </summary>
    /// <param name="p_TransformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static int GetRemaining(Dictionary<string, string> p_TransformedHeaders)
    {
        foreach (var l_KVP in p_TransformedHeaders)
        {
            var l_Lower = l_KVP.Key.ToLower();
            if (   l_Lower == "x-rate-limit-remaining" || l_Lower == "x-ratelimit-remaining"
                || l_Lower == "rate-limit-remaining"   || l_Lower == "ratelimit-remaining")
            {
                if (int.TryParse(l_KVP.Value, out var l_Value))
                    return l_Value;
                else
                    return -1;
            }
        }

        return -1;
    }
    /// <summary>
    /// Get reset time from header
    /// </summary>
    /// <param name="p_TransformedHeaders">Transformed headers</param>
    /// <returns></returns>
    private static DateTime GetReset(Dictionary<string, string> p_TransformedHeaders)
    {
        foreach (var l_KVP in p_TransformedHeaders)
        {
            var l_Lower = l_KVP.Key.ToLower();
            if (   l_Lower == "x-rate-limit-reset" || l_Lower == "x-ratelimit-reset"
                || l_Lower == "rate-limit-reset"   || l_Lower == "ratelimit-reset")
            {
                if (!long.TryParse(l_KVP.Value, out var l_Value))
                    return DateTime.Now.AddSeconds(2);

                if (l_Value < 1000000000)
                    return Misc.Time.FromUnixTime(Misc.Time.UnixTimeNow() + l_Value);

                return Misc.Time.FromUnixTime(l_Value);
            }
        }

        return DateTime.Now.AddSeconds(2);
    }
}
