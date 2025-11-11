using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.HTTPClient;

/// <summary>
/// Web Client interface
/// </summary>
public interface IHTTPClient
{
    [Flags]
    public enum EOptions
    {
        None               = 0,
        ForceCacheDiscard  = 1 << 0,
        KeepAlive          = 1 << 1,
        NoRetryOnRateLimit = 1 << 2,
    }
    /// <summary>
    /// Options for requests
    /// </summary>
    [Flags]
    public enum ERequestOptions
    {
        None                = 0,
        IgnoreRetryPolicy   = 1 << 0,
        NoRetryOnRateLimit  = 1 << 1,
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public abstract EOptions           Options        { get; }
    public abstract int                MaxRetry       { get; set; }
    public abstract TimeSpan           RetryInterval  { get; set; }
    public abstract HttpRequestHeaders GlobalHeaders  { get; }
    public abstract CookieContainer?   CookieJar      { get; set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do a sync request
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HTTP method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns>The response if the request reached the server</returns>
    public HTTPClientResponse? DoRequest(
            string             method,
            string             url,
            HTTPClientPayload? payload         = null,
            ERequestOptions    options         = ERequestOptions.None,
            IProgress<float>?  progressHandler = null
        );
    /// <summary>
    /// Do a non blocking request in the background with a callback
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HTTP method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    public void DoRequestInBackground(
            string                       method,
            string                       url,
            CancellationToken            cancellationToken,
            Action<HTTPClientResponse?>? callback,
            HTTPClientPayload?           payload           = null,
            ERequestOptions              options           = ERequestOptions.None,
            IProgress<float>?            progressHandler   = null
        );
    /// <summary>
    /// Do a async request
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HTTP method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns>The response if the request reached the server</returns>
    public Task<HTTPClientResponse?> DoRequestAsync(
            string             method,
            string             url,
            CancellationToken  cancellationToken,
            HTTPClientPayload? payload           = null,
            ERequestOptions    options           = ERequestOptions.None,
            IProgress<float>?  progressHandler   = null
        );
}
