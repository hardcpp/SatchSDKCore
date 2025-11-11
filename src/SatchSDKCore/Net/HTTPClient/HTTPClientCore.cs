using SSC.Misc;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using static SSC.Net.HTTPClient.IHTTPClient;

namespace SSC.Net.HTTPClient;

public class HTTPClientCore : IHTTPClient
{
    /// <summary>
    /// Global client instance
    /// </summary>
    public static readonly HTTPClientCore GlobalClient = new HTTPClientCore("", TimeSpan.FromSeconds(10));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private HttpClient        _client;
    private HttpClientHandler _clientHandler;
    private CookieContainer?  _cookieContainer;
    private EOptions          _options;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public EOptions           Options         => _options;
    public int                MaxRetry        { get; set; } = 2;
    public TimeSpan           RetryInterval   { get; set; } = TimeSpan.FromSeconds(5);
    public HttpRequestHeaders GlobalHeaders   => _client.DefaultRequestHeaders;
    public CookieContainer?   CookieJar       {
        get => _cookieContainer;
        set {
            _cookieContainer = value;

            if (_cookieContainer != null)
                _clientHandler.CookieContainer = _cookieContainer;

            _clientHandler.UseCookies = _cookieContainer != null;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="baseURL">Base address</param>
    /// <param name="timeout">Maximum timeout</param>
    public HTTPClientCore(string baseURL, TimeSpan timeout, EOptions options = EOptions.KeepAlive)
    {
        _clientHandler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All
        };

        _client = new HttpClient(_clientHandler)
        {
            Timeout               = timeout,
            DefaultRequestVersion = HttpVersion.Version10,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrHigher
        };

        if (!string.IsNullOrEmpty(baseURL))
            _client.BaseAddress = new Uri(baseURL);

        if (_options.HasFlag(EOptions.ForceCacheDiscard))
        {
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache         = true,
                NoStore         = false,
                MustRevalidate  = true,
                ProxyRevalidate = true,
                MaxAge          = TimeSpan.FromSeconds(0),
                SharedMaxAge    = TimeSpan.FromMilliseconds(0),
                MaxStaleLimit   = TimeSpan.FromMilliseconds(0)
            };
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        _client.DefaultRequestHeaders.ConnectionClose = !_options.HasFlag(EOptions.KeepAlive);
        _client.DefaultRequestHeaders.Add("User-Agent", $"SatchSDKCore/{version}");
    }

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
    public HTTPClientResponse? DoRequest(
            string             method,
            string             url,
            HTTPClientPayload? payload         = null,
            ERequestOptions    options         = ERequestOptions.None,
            IProgress<float>?  progressHandler = null
        )
    {
        bool                isQueryCompleted = false;
        HTTPClientResponse? result           = null;

        DoRequestImpl(method, url, null, CancellationToken.None, (p_Result) => { result = p_Result; isQueryCompleted = true; }, options, progressHandler).ConfigureAwait(false);

        while (!isQueryCompleted)
            Thread.Sleep(5);

        return result;
    }
    /// <summary>
    /// Do a non blocking request in the background with a callback
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HTTP method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="payload">Request payload</param>
    /// <param name="callback">Callback</param>
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
        )
    {
        DoRequestImpl(method, url, payload, cancellationToken, callback, options, progressHandler).ConfigureAwait(false);
    }
    /// <summary>
    /// Do a async request
    /// </summary>
    /// <param name="url">Target URL</param>
    /// <param name="method">HTTP method GET/POST/PATCH/PUT/DELETE/OPTION...</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="payload">Request payload</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    public async Task<HTTPClientResponse?> DoRequestAsync(
            string             method,
            string             url,
            CancellationToken  cancellationToken,
            HTTPClientPayload? payload           = null,
            ERequestOptions    options           = ERequestOptions.None,
            IProgress<float>?  progressHandler   = null
        )
    {
        return await DoRequestImpl(method, url, null, cancellationToken, null, options, progressHandler).ConfigureAwait(false);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Safe URL parsing
    /// </summary>
    /// <param name="p_URL"></param>
    /// <returns></returns>
    private string SafeURL(string p_URL)
    {
        var l_Result = p_URL;

        if (!p_URL.Contains("://"))
            l_Result = _client.BaseAddress + l_Result;

        if (l_Result.Contains("?"))
            l_Result = l_Result.Substring(0, l_Result.IndexOf("?"));

        return l_Result;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do request
    /// </summary>
    /// <param name="method">Http method</param>
    /// <param name="url">Target URL</param>
    /// <param name="payload">Content to post</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns></returns>
    private async ValueTask<HTTPClientResponse?> DoRequestImpl(
            string                       method,
            string                       url,
            HTTPClientPayload?           payload,
            CancellationToken            cancellationToken,
            Action<HTTPClientResponse?>? callback,
            ERequestOptions              options,
            IProgress<float>?            progressHandler
        )
    {
#if DEBUG
        Logging.Log(ELogSeverity.Debug, $"[CP_SDK.Network][WebClientCore] {method} " + url);
#endif

        HTTPClientResponse? lastResponse = null;
        ENestedFlowControl  flowControl;

        for (int currentRetryI = 0; currentRetryI < MaxRetry; currentRetryI++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (currentRetryI > 0 && options.HasFlag(ERequestOptions.IgnoreRetryPolicy))
                break;

            var baseHttpResponse = null as HttpResponseMessage;
            try
            {
                progressHandler?.Report(0.0f);

                lastResponse = null;
                baseHttpResponse = await PrepareAndStartRequest(method, url, payload, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                lastResponse = new HTTPClientResponse(baseHttpResponse);

                if (lastResponse.IsRateLimited)
                {
                    flowControl = await HandleRateLimit(baseHttpResponse, lastResponse, cancellationToken, options);
                    if (flowControl == ENestedFlowControl.Loop)
                        continue;
                    else if (flowControl == ENestedFlowControl.Break || flowControl == ENestedFlowControl.Return)
                        break;
                }

                await HandleResponse(baseHttpResponse, lastResponse, cancellationToken, options, progressHandler);
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!lastResponse.IsSuccessStatusCode)
                {
                    if (!lastResponse.ShouldRetry || options.HasFlag(ERequestOptions.IgnoreRetryPolicy))
                    {
                        Logging.Log(ELogSeverity.Debug, $"[CP_SDK.Network][WebClientCore] Request {SafeURL(url)} failed with code {lastResponse.StatusCode}:\"{lastResponse.ReasonPhrase}\", not retrying");
                        break;
                    }

                    Logging.Log(ELogSeverity.Debug, $"[CP_SDK.Network][WebClientCore] Request {SafeURL(url)} failed with code {lastResponse.StatusCode}:\"{lastResponse.ReasonPhrase}\", next try in {RetryInterval} seconds...");

                    await Task.Delay(RetryInterval, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    continue;
                }

                progressHandler?.Report(1.0f);
                break;
            }
            catch (Exception)
            {
                /// Do nothing here
            }
            finally
            {
                baseHttpResponse?.Dispose();
            }
        }

        if (cancellationToken.IsCancellationRequested)
            lastResponse = null;

        callback?.Invoke(lastResponse);
        return lastResponse;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Prepare and start a request
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="url">URL to request</param>
    /// <param name="payload">Request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The prepared and started core http request resultResponse</returns>
    /// <exception cref="ArgumentException">If the method is not supported/implemented</exception>
    private async ValueTask<HttpResponseMessage> PrepareAndStartRequest(
            string              method,
            string              url,
            HTTPClientPayload?  payload,
            CancellationToken   cancellationToken
        )
    {
        switch (method)
        {
            case "GET":
                return await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            case "POST":
                ByteArrayContent? postContent = null;
                if (payload != null)
                {
                    postContent = new ByteArrayContent(payload.Bytes);
                    postContent.Headers.ContentType = null;
                    postContent.Headers.Remove("Content-Type");
                    postContent.Headers.TryAddWithoutValidation("Content-Type", payload.Type);
                }

                return await _client.PostAsync(url, postContent, cancellationToken).ConfigureAwait(false);

            case "PATCH":
            case "PUT":
                ByteArrayContent? patchPutContent = null;
                if (payload != null)
                {
                    patchPutContent = new ByteArrayContent(payload.Bytes);
                    patchPutContent.Headers.ContentType = null;
                    patchPutContent.Headers.Remove("Content-Type");
                    patchPutContent.Headers.TryAddWithoutValidation("Content-Type", payload.Type);
                }

                return await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = patchPutContent, }, cancellationToken).ConfigureAwait(false);

            case "DELETE":
                return await _client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        }

        throw new ArgumentException($"Unsuported HTTP method '{method}' in {nameof(HTTPClientCore)}");
    }
    /// <summary>
    /// Handle the rate limit response
    /// </summary>
    /// <param name="baseHttpResponse">Base core http response</param>
    /// <param name="resultResponse">Result resultResponse at the end of the DoRequest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Request options</param>
    /// <returns>ENestedFlowControl for the caller</returns>
    private async ValueTask<ENestedFlowControl> HandleRateLimit(
            HttpResponseMessage baseHttpResponse,
            HTTPClientResponse  resultResponse,
            CancellationToken   cancellationToken,
            ERequestOptions     options
        )
    {
        var rateLimitInfo = HTTPClientRateLimitInfo.Get(baseHttpResponse);
        if (rateLimitInfo == null)
        {
            resultResponse.DangerousSetRateLimit(rateLimitInfo);
            return ENestedFlowControl.Break;
        }

        int remainingMilliseconds = (int)(rateLimitInfo.Reset - DateTime.Now).TotalMilliseconds;
        if (remainingMilliseconds <= 0)
            return ENestedFlowControl.None;

        if (options.HasFlag(ERequestOptions.NoRetryOnRateLimit) || _options.HasFlag(EOptions.NoRetryOnRateLimit))
            return ENestedFlowControl.Break;

        Logging.Log(ELogSeverity.Debug, $"[CP_SDK.Network][WebClientCore] Request {SafeURL("todo")} was rate limited, retrying in {remainingMilliseconds}ms...");

        await Task.Delay(remainingMilliseconds, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            resultResponse.DangerousSetRateLimit(rateLimitInfo);
            return ENestedFlowControl.Break;
        }

        return ENestedFlowControl.Loop;
    }
    /// <summary>
    /// Handle the resultResponse from the core http client
    /// </summary>
    /// <param name="baseHttpResponse">Base core http response</param>
    /// <param name="resultResponse">Result resultResponse at the end of the DoRequest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Request options</param>
    /// <param name="progressHandler">Progress reporter</param>
    private async ValueTask HandleResponse(
            HttpResponseMessage baseHttpResponse,
            HTTPClientResponse  resultResponse,
            CancellationToken   cancellationToken,
            ERequestOptions     options,
            IProgress<float>?   progressHandler
        )
    {
        if (progressHandler == null)
        {
            resultResponse.DangerousPopulate(await baseHttpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
            return;
        }

        var memoryStream   = new MemoryStream();
        var responseStream = await baseHttpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var readBuffer     = new byte[8 * 1024];
        var contentLength  = baseHttpResponse.Content.Headers.ContentLength;
        var totalReaded    = 0L;

        // TODO handle chunked encoding
        try
        {
            while (true)
            {
                int currentReaded;
                if ((currentReaded = await responseStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await memoryStream.WriteAsync(readBuffer, 0, currentReaded, cancellationToken).ConfigureAwait(false);
                    totalReaded += currentReaded;

                    if (contentLength.HasValue)
                        progressHandler?.Report(totalReaded / (float)contentLength.Value);
                }
                else
                {
                    progressHandler?.Report(1.0f);
                    resultResponse.DangerousPopulate(memoryStream.ToArray());
                    break;
                }
            }
        }
        finally
        {
            responseStream.Dispose();
            memoryStream.Dispose();
        }
    }
}
