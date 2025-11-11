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
    public static readonly HTTPClientCore GlobalClient = new("", TimeSpan.FromSeconds(10));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly HttpClient        _client;
    private readonly HttpClientHandler _clientHandler;

    private CookieContainer? _cookieContainer;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public EOptions           Options         { get; private set; }
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
        Options = options;

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

        if (Options.HasFlag(EOptions.ForceCacheDiscard))
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

        _client.DefaultRequestHeaders.ConnectionClose = !Options.HasFlag(EOptions.KeepAlive);
        _client.DefaultRequestHeaders.Add("User-Agent", $"SatchSDKCore/{version}");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc/>
    public HTTPClientResponse? DoRequest(
        string                  method,
        string                  url,
        HTTPClientPayload?      payload         = null,
        ERequestOptions         options         = ERequestOptions.None,
        IHTTPClientDataHandler? dataHandler     = null,
        IProgress<float>?       progressHandler = null
    )
    {
        var task = DoRequestImpl(method, url, null, CancellationToken.None, null, options, dataHandler, progressHandler);
        task.Wait();

        return task.Result;
    }
    /// <inheritdoc/>
    public void DoRequestInBackground(
        string                       method,
        string                       url,
        CancellationToken            cancellationToken,
        Action<HTTPClientResponse?>? callback,
        HTTPClientPayload?           payload           = null,
        ERequestOptions              options           = ERequestOptions.None,
        IHTTPClientDataHandler?      dataHandler       = null,
        IProgress<float>?            progressHandler   = null
    ) => DoRequestImpl(method, url, payload, cancellationToken, callback, options, dataHandler, progressHandler).ConfigureAwait(false);
    /// <inheritdoc/>
    public async Task<HTTPClientResponse?> DoRequestAsync(
        string                  method,
        string                  url,
        CancellationToken       cancellationToken,
        HTTPClientPayload?      payload           = null,
        ERequestOptions         options           = ERequestOptions.None,
        IHTTPClientDataHandler? dataHandler       = null,
        IProgress<float>?       progressHandler   = null
    ) => await DoRequestImpl(method, url, payload, cancellationToken, null, options, dataHandler, progressHandler).ConfigureAwait(false);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Safe URL parsing
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private string SafeURL(string url)
    {
        var result = url;

        if (!url.Contains("://"))
            result = _client.BaseAddress + result;

        if (result.Contains('?'))
            result = result[..result.IndexOf('?', StringComparison.Ordinal)];

        return result;
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
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    /// <returns></returns>
    private async Task<HTTPClientResponse?> DoRequestImpl(
        string                       method,
        string                       url,
        HTTPClientPayload?           payload,
        CancellationToken            cancellationToken,
        Action<HTTPClientResponse?>? callback,
        ERequestOptions              options,
        IHTTPClientDataHandler?      dataHandler,
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

                lastResponse     = null;
                baseHttpResponse = await PrepareAndStartRequest(method, url, payload, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                lastResponse = new HTTPClientResponse(baseHttpResponse);

                if (lastResponse.IsRateLimited)
                {
                    flowControl = await HandleRateLimit(baseHttpResponse, lastResponse, cancellationToken, options);
                    if (flowControl == ENestedFlowControl.Loop)
                        continue;
                    else if (flowControl == ENestedFlowControl.Break || flowControl == ENestedFlowControl.Return)
                        break;
                }

                await HandleResponse(baseHttpResponse, lastResponse, cancellationToken, options, dataHandler, progressHandler);
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
        ByteArrayContent? content = null;
        if (payload != null)
        {
            content = new ByteArrayContent(payload.Bytes);
            content.Headers.ContentType = null;
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", payload.Type);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Parse(method), url);
        requestMessage.Content = content;

        return await _client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
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
            // TODO log unable to get rate limit info
            return ENestedFlowControl.Break;
        }

        int remainingMilliseconds = (int)(rateLimitInfo.Reset - DateTime.Now).TotalMilliseconds;
        if (remainingMilliseconds <= 0)
            return ENestedFlowControl.None;

        if (options.HasFlag(ERequestOptions.NoRetryOnRateLimit) || Options.HasFlag(EOptions.NoRetryOnRateLimit))
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
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    private async ValueTask HandleResponse(
        HttpResponseMessage     baseHttpResponse,
        HTTPClientResponse      resultResponse,
        CancellationToken       cancellationToken,
        ERequestOptions         options,
        IHTTPClientDataHandler? dataHandler,
        IProgress<float>?       progressHandler
    )
    {
        var memoryStream   = new MemoryStream();
        var responseStream = await baseHttpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var readBuffer     = new byte[dataHandler?.IdealBufferSize ?? 8 * 1024];
        var contentLength  = baseHttpResponse.Content.Headers.ContentLength;
        var totalReaded    = 0L;

        // TODO handle chunked encoding
        try
        {
            dataHandler?.Begin();

            while (true)
            {
                int currentReaded;
                if ((currentReaded = await responseStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    // Handle potential late cancel at the end of ReadAsync
                    cancellationToken.ThrowIfCancellationRequested();

                    if (dataHandler != null)
                        await dataHandler.ProcessAsync(readBuffer.AsSpan(0, currentReaded), contentLength.HasValue ? contentLength.Value : null).ConfigureAwait(false);
                    else
                        await memoryStream.WriteAsync(readBuffer, 0, currentReaded, cancellationToken).ConfigureAwait(false);

                    totalReaded += currentReaded;

                    if (contentLength.HasValue)
                        progressHandler?.Report(totalReaded / (float)contentLength.Value);
                }
                else
                {
                    progressHandler?.Report(1.0f);

                    if (dataHandler != null)
                    {
                        dataHandler.End();
                        resultResponse.DangerousSetBodyDataHandler(dataHandler);
                    }
                    else
                        resultResponse.DangerousSetBodyBytes(memoryStream.ToArray());

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
