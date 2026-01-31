using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SSC.Net.HttpEx;

/// <summary>
/// Web Response class
/// </summary>
public sealed class HttpClientExResponse
{
    private static readonly byte[] s_UTF8Preamble = Encoding.UTF8.GetPreamble();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private byte[]? _bodyBytes = null;
    private IHttpClientExDataHandler? _bodyDataHandler = null;
    private string? _bodyString = null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HttpStatusCode StatusCode;
    public readonly string? ReasonPhrase;
    public HttpClientExRateLimitInfo? RateLimitInfo { get; private set; }
    public readonly bool IsSuccessStatusCode;
    public readonly bool ShouldRetry;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public bool IsRateLimited => !IsSuccessStatusCode && StatusCode == (HttpStatusCode)429;
    public byte[]? BodyBytes => _bodyBytes;
    public IHttpClientExDataHandler? BodyHandler => _bodyDataHandler;
    public string? BodyString
    {
        get
        {
            if (_bodyString != null)
                return _bodyString;

            if (_bodyBytes == null)
            {
                _bodyString = null;
                return _bodyString;
            }
            else if (_bodyBytes.Length == 0)
            {
                _bodyString = string.Empty;
                return _bodyString;
            }

            if (s_UTF8Preamble.Length > 0 && _bodyBytes!.Length >= s_UTF8Preamble.Length && _bodyBytes.Take(s_UTF8Preamble.Length).SequenceEqual(s_UTF8Preamble))
                _bodyString = Encoding.UTF8.GetString(_bodyBytes, s_UTF8Preamble.Length, _bodyBytes.Length - s_UTF8Preamble.Length);
            else
                _bodyString = Encoding.UTF8.GetString(_bodyBytes!);

            return _bodyString;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Explicit constructor
    /// </summary>
    /// <param name="statusCode">Result status code</param>
    /// <param name="reasonPhrase">Code reason if any</param>
    /// <param name="isSuccessStatusCode">If the status code considered success?</param>
    public HttpClientExResponse(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        bool isSuccessStatusCode
    )
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        IsSuccessStatusCode = isSuccessStatusCode;
        ShouldRetry = IsSuccessStatusCode ? false : (int)statusCode < 400 || (int)statusCode >= 500;
    }
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="coreHttpResponse">Reply status</param>
    public HttpClientExResponse(HttpResponseMessage coreHttpResponse)
    {
        StatusCode = coreHttpResponse.StatusCode;
        ReasonPhrase = coreHttpResponse.ReasonPhrase;
        IsSuccessStatusCode = coreHttpResponse.IsSuccessStatusCode;
        ShouldRetry = IsSuccessStatusCode ? false : (int)coreHttpResponse.StatusCode < 400 || (int)coreHttpResponse.StatusCode >= 500;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Set the body bytes
    /// </summary>
    /// <param name="bodyBytes">New body bytes</param>
    /// <exception cref="InvalidOperationException">If the content have already been set</exception>
    public void DangerousSetBodyBytes(byte[] bodyBytes)
    {
        ArgumentNullException.ThrowIfNull(bodyBytes);

        if (_bodyBytes != null || _bodyDataHandler != null)
            throw new InvalidOperationException("Can not alter HTTPClientResponse body after initial set");

        _bodyBytes = bodyBytes;
        _bodyDataHandler = null;
        _bodyString = null;
    }
    /// <summary>
    /// Set the body data handler
    /// </summary>
    /// <param name="bodyDataHandler">New body data handler</param>
    /// <exception cref="InvalidOperationException">If the content have already been set</exception>
    public void DangerousSetBodyDataHandler(IHttpClientExDataHandler bodyDataHandler)
    {
        ArgumentNullException.ThrowIfNull(bodyDataHandler);

        if (_bodyBytes != null || _bodyDataHandler != null)
            throw new InvalidOperationException("Can not alter HTTPClientResponse body after initial set");

        _bodyBytes = null;
        _bodyDataHandler = bodyDataHandler;
        _bodyString = null;
    }
    /// <summary>
    /// Set rate limit info
    /// </summary>
    /// <param name="rateLimitInfo">New rate limit info</param>
    public void DangerousSetRateLimit(HttpClientExRateLimitInfo rateLimitInfo)
    {
        RateLimitInfo = rateLimitInfo;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get JObject from serialized JSON
    /// </summary>
    /// <param name="resultJObject">Result object</param>
    /// <returns></returns>
    public bool TryAsJObject(out JObject? resultJObject)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(BodyString);

        resultJObject = null;
        try
        {
            resultJObject = JObject.Parse(BodyString);
        }
        catch (Exception) { return false; }

        return resultJObject != null;
    }
    /// <summary>
    /// Get JObject from serialized JSON
    /// </summary>
    /// <param name="resultObject">Result object</param>
    /// <returns></returns>
    public bool TryGetObject<T>(out T? resultObject)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNullOrEmpty(BodyString);

        resultObject = null;
        try
        {
            resultObject = JsonConvert.DeserializeObject<T>(BodyString);
        }
        catch (Exception) { return false; }

        return resultObject != null;
    }
}
