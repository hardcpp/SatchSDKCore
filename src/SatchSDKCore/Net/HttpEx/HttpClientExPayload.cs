using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web;
using System.Globalization;
using System.Text;
using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http client payload
/// </summary>
public class HttpClientExPayload
{
    public static HttpClientExPayload Empty = new HttpClientExPayload(Array.Empty<byte>(), "");

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Content
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Content type
    /// </summary>
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
        foreach ((var key, var value) in formFields)
        {
            if (content.Length != 0)
                content.Append("&");

            content.Append(string.Format(CultureInfo.InvariantCulture, "{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)));
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
        => new HttpClientExPayload(Encoding.UTF8.GetBytes(content), $"application/json; charset=utf-8");
    /// <summary>
    /// Constructor from Json
    /// </summary>
    /// <param name="content">Json content</param>
    /// <param name="indent">Should indent?</param>
    /// <returns></returns>
    public static HttpClientExPayload FromJson(JObject content, bool indent = false)
        => new HttpClientExPayload(Encoding.UTF8.GetBytes(content.ToString(indent ? Formatting.Indented : Formatting.None)), $"application/json; charset=utf-8");
    /// <summary>
    /// Constructor from Json
    /// </summary>
    /// <param name="content">Json content</param>
    /// <param name="indend">Should indent?</param>
    /// <returns></returns>
    public static HttpClientExPayload FromJson(object content, bool indend = false)
        => new HttpClientExPayload(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content, indend ? Formatting.Indented : Formatting.None)), $"application/json; charset=utf-8");
}
