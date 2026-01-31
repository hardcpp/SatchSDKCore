using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advance Http Client interface
/// </summary>
public interface IHttpClientEx : IDisposable
{
    /// <summary>
    /// Global client options
    /// </summary>
    [Flags]
    enum EOptions
    {
        None = 0,
        ForceCacheDiscard = 1 << 0,
        KeepAlive = 1 << 1,
        NoRetryOnRateLimit = 1 << 2,
    }
    /// <summary>
    /// Options for requests
    /// </summary>
    [Flags]
    enum ERequestOptions
    {
        None = 0,
        IgnoreRetryPolicy = 1 << 0,
        NoRetryOnRateLimit = 1 << 1,
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    abstract EOptions Options { get; }
    abstract int MaxRetry { get; set; }
    abstract TimeSpan RetryInterval { get; set; }
    abstract HttpRequestHeaders GlobalHeaders { get; }
    abstract CookieContainer? CookieJar { get; set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do a sync request
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HttpServerEx method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns>The response if the request reached the server</returns>
    HttpClientExResponse DoRequest(
        string method,
        string url,
        HttpClientExPayload? payload = null,
        ERequestOptions options = ERequestOptions.None,
        IHttpClientExDataHandler? dataHandler = null,
        IProgress<float>? progressHandler = null
    );
    /// <summary>
    /// Do a non-blocking request in the background with a callback
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HttpServerEx method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback, Response is null if the implementation fail to request</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    void DoRequestInBackground(
        string method,
        string url,
        CancellationToken cancellationToken,
        Action<HttpClientExResponse?>? callback,
        HttpClientExPayload? payload = null,
        ERequestOptions options = ERequestOptions.None,
        IHttpClientExDataHandler? dataHandler = null,
        IProgress<float>? progressHandler = null
    );
    /// <summary>
    /// Do an async request
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HttpServerEx method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns>The response if the request reached the server</returns>
    Task<HttpClientExResponse> DoRequestAsync(
        string method,
        string url,
        CancellationToken cancellationToken,
        HttpClientExPayload? payload = null,
        ERequestOptions options = ERequestOptions.None,
        IHttpClientExDataHandler? dataHandler = null,
        IProgress<float>? progressHandler = null
    );
}
