using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http client payload
/// </summary>
public class HttpClientExPayload
{
    private static readonly JsonSerializerOptions s_RegularSerialize  = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions s_IndentedSerialize = new() { WriteIndented = true };

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public static HttpClientExPayload Empty = new(Array.Empty<byte>(), "");

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public byte[] Bytes;
    public string Type;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bytes">Bytes</param>
    /// <param name="type">Content</param>
    private HttpClientExPayload(byte[] bytes, string type)
    {
        Bytes = bytes;
        Type  = type;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor from form
    /// </summary>
    /// <param name="formFields">Form fields</param>
    /// <returns></returns>
    public static HttpClientExPayload FromForm(IReadOnlyDictionary<string, string> formFields)
    {
        var content = new StringBuilder(1024);
        foreach ((string key, string value) in formFields)
        {
            if (content.Length != 0)
                content.Append('&');

            content.Append(string.Format(CultureInfo.InvariantCulture, "{0}={1}", HttpUtility.UrlEncode(key),
                                         HttpUtility.UrlEncode(value)));
        }

        return new HttpClientExPayload(Encoding.UTF8.GetBytes(content.ToString()), "application/x-www-form-urlencoded");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor from Json
    /// </summary>
    /// <param name="content">Json content</param>
    /// <returns></returns>
    public static HttpClientExPayload FromJsonString(string content)
        => new(Encoding.UTF8.GetBytes(content), "application/json; charset=utf-8");

    /// <summary>
    /// Constructor from Json
    /// </summary>
    /// <param name="content">Json content</param>
    /// <param name="indent">Should indent?</param>
    /// <returns></returns>
    public static HttpClientExPayload FromJson(JsonNode content)
        => new(Encoding.UTF8.GetBytes(content.ToJsonString(SDKConfig.JsonSerializerOptions)),
               "application/json; charset=utf-8");
}
