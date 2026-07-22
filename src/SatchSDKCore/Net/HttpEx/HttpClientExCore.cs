using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SSC.Misc;
using static SSC.Net.HttpEx.IHttpClientEx;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Client implementation using dotnet core implementation
/// </summary>
public class HttpClientExCore : IHttpClientEx, IDisposable
{
    public static readonly HttpClientExCore GlobalClient = new("", TimeSpan.FromSeconds(10));

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly HttpClient        _client;
    private readonly HttpClientHandler _clientHandler;

    private CookieContainer? _cookieContainer;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////


    public EOptions           Options       { get; }
    public int                MaxRetry      { get; set; } = 2;
    public TimeSpan           RetryInterval { get; set; } = TimeSpan.FromSeconds(5);
    public HttpRequestHeaders GlobalHeaders => _client.DefaultRequestHeaders;

    public CookieContainer? CookieJar
    {
        get => _cookieContainer;
        set
        {
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
    public HttpClientExCore(string baseURL, TimeSpan timeout, EOptions options = EOptions.KeepAlive)
    {
        Options = options;

        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _client = new HttpClient(_clientHandler)
        {
            Timeout               = timeout,
            DefaultRequestVersion = HttpVersion.Version11,
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

        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

        _client.DefaultRequestHeaders.ConnectionClose = !Options.HasFlag(EOptions.KeepAlive);
        _client.DefaultRequestHeaders.Add("User-Agent", $"SatchSDKCore/{version}");
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public HttpClientExResponse DoRequest(
        string                    method,
        string                    url,
        HttpClientExPayload?      payload         = null,
        ERequestOptions           options         = ERequestOptions.None,
        IHttpClientExDataHandler? dataHandler     = null,
        IProgress<float>?         progressHandler = null
    )
    {
        Task<HttpClientExResponse> task = DoRequestImpl(method, url, payload, CancellationToken.None, null, options,
                                                        dataHandler, progressHandler);
        task.Wait();

        return task.Result;
    }

    /// <inheritdoc />
    public void DoRequestInBackground(
        string                         method,
        string                         url,
        CancellationToken              cancellationToken,
        Action<HttpClientExResponse?>? callback,
        HttpClientExPayload?           payload         = null,
        ERequestOptions                options         = ERequestOptions.None,
        IHttpClientExDataHandler?      dataHandler     = null,
        IProgress<float>?              progressHandler = null
    ) => DoRequestImpl(method, url, payload, cancellationToken, callback, options, dataHandler, progressHandler)
        .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpClientExResponse> DoRequestAsync(
        string                    method,
        string                    url,
        CancellationToken         cancellationToken,
        HttpClientExPayload?      payload         = null,
        ERequestOptions           options         = ERequestOptions.None,
        IHttpClientExDataHandler? dataHandler     = null,
        IProgress<float>?         progressHandler = null
    ) => await DoRequestImpl(method, url, payload, cancellationToken, null, options, dataHandler, progressHandler)
        .ConfigureAwait(false);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Safe URL parsing
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private string SafeURL(string url)
    {
        string result = url;

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
    private async Task<HttpClientExResponse> DoRequestImpl(
        string                         method,
        string                         url,
        HttpClientExPayload?           payload,
        CancellationToken              cancellationToken,
        Action<HttpClientExResponse?>? callback,
        ERequestOptions                options,
        IHttpClientExDataHandler?      dataHandler,
        IProgress<float>?              progressHandler
    )
    {
#if DEBUG
        Logging.Log(ELogSeverity.Debug, $"[Net.HttpEx][HttpClientExCore] {method} " + url);
#endif

        Exception?            lastException = null;
        HttpClientExResponse? lastResponse  = null;
        ENestedFlowControl    flowControl;

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
                baseHttpResponse = await PrepareAndStartRequest(method, url, payload, cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                lastResponse = new HttpClientExResponse(baseHttpResponse);

                if (lastResponse.IsRateLimited)
                {
                    flowControl = await HandleRateLimit(baseHttpResponse, lastResponse, cancellationToken, options);
                    if (flowControl == ENestedFlowControl.Loop)
                        continue;
                    else if (flowControl == ENestedFlowControl.Break || flowControl == ENestedFlowControl.Return)
                        break;
                }

                await HandleResponse(baseHttpResponse, lastResponse, cancellationToken, options, dataHandler,
                                     progressHandler);
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!lastResponse.IsSuccessStatusCode)
                {
                    if (!lastResponse.ShouldRetry || options.HasFlag(ERequestOptions.IgnoreRetryPolicy))
                    {
                        Logging.Log(ELogSeverity.Debug,
                                    $"[Net.HttpEx][HttpClientExCore] Request {SafeURL(url)} failed with code {lastResponse.StatusCode}:\"{lastResponse.ReasonPhrase}\", not retrying");
                        break;
                    }

                    Logging.Log(ELogSeverity.Debug,
                                $"[Net.HttpEx][HttpClientExCore] Request {SafeURL(url)} failed with code {lastResponse.StatusCode}:\"{lastResponse.ReasonPhrase}\", next try in {RetryInterval} seconds...");

                    if (currentRetryI != MaxRetry - 1)
                        await Task.Delay(RetryInterval, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                progressHandler?.Report(1.0f);
                lastException = null;
                break;
            }
            catch (Exception exception)
            {
                if (currentRetryI != MaxRetry - 1)
                {
                    Logging.Log(ELogSeverity.Debug,
                                $"[Net.HttpEx][HttpClientExCore] Request failed, retry in {RetryInterval.TotalSeconds} seconds...");
                    await Task.Delay(RetryInterval, cancellationToken).ConfigureAwait(false);
                }

                lastException = exception;
            }
            finally
            {
                baseHttpResponse?.Dispose();
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            lastResponse  = null;
            lastException = new TaskCanceledException();
        }

        callback?.Invoke(lastResponse);

        if (lastException != null)
        {
            Logging.Log(ELogSeverity.Error, "[Net.HttpEx][HttpClientExCore] Request failed:");
            Logging.Log(ELogSeverity.Error, lastException);

            ExceptionDispatchInfo.Capture(lastException).Throw();
        }

        return lastResponse!;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Prepare and start a request
    /// </summary>
    /// <param name="method">HttpServerEx method</param>
    /// <param name="url">URL to request</param>
    /// <param name="payload">Request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The prepared and started core http request resultResponse</returns>
    /// <exception cref="ArgumentException">If the method is not supported/implemented</exception>
    private async ValueTask<HttpResponseMessage> PrepareAndStartRequest(
        string               method,
        string               url,
        HttpClientExPayload? payload,
        CancellationToken    cancellationToken
    )
    {
        ByteArrayContent? content = null;
        if (payload != null)
        {
            content                     = new ByteArrayContent(payload.Bytes);
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
    /// <param name="resultResponse">Result resultResponse at the end of the DoCall</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Request options</param>
    /// <returns>ENestedFlowControl for the caller</returns>
    private async ValueTask<ENestedFlowControl> HandleRateLimit(
        HttpResponseMessage  baseHttpResponse,
        HttpClientExResponse resultResponse,
        CancellationToken    cancellationToken,
        ERequestOptions      options
    )
    {
        HttpClientExRateLimitInfo? rateLimitInfo = HttpClientExRateLimitInfo.Get(baseHttpResponse);
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

        Logging.Log(ELogSeverity.Debug,
                    $"[Net.HttpEx][HttpClientExCore] Request {SafeURL("todo")} was rate limited, retrying in {remainingMilliseconds}ms...");

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
    /// <param name="resultResponse">Result resultResponse at the end of the DoCall</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Request options</param>
    /// <param name="dataHandler">Optional data handler</param>
    /// <param name="progressHandler">Progress reporter</param>
    private async ValueTask HandleResponse(
        HttpResponseMessage       baseHttpResponse,
        HttpClientExResponse      resultResponse,
        CancellationToken         cancellationToken,
        ERequestOptions           options,
        IHttpClientExDataHandler? dataHandler,
        IProgress<float>?         progressHandler
    )
    {
        using (Stream responseStream =
               await baseHttpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            using (MemoryStream? memoryStream = dataHandler != null ? null : new MemoryStream())
            {
                byte[] readBuffer    = new byte[dataHandler?.IdealBufferSize ?? 8 * 1024];
                long?  contentLength = baseHttpResponse.Content.Headers.ContentLength;
                long   totalReaded   = 0L;

                // TODO handle chunked encoding
                dataHandler?.Begin();

                while (true)
                {
                    int currentReaded;
                    if ((currentReaded = await responseStream
                            .ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken)
                            .ConfigureAwait(false)) > 0)
                    {
                        // Handle potential late cancel at the end of ReadAsync
                        cancellationToken.ThrowIfCancellationRequested();

                        if (dataHandler != null)
                        {
                            await dataHandler.ProcessAsync(readBuffer.AsSpan(0, currentReaded),
                                                           contentLength.HasValue ? contentLength.Value : null)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await memoryStream!.WriteAsync(readBuffer, 0, currentReaded, cancellationToken)
                                .ConfigureAwait(false);
                        }

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
                            resultResponse.DangerousSetBodyBytes(memoryStream!.ToArray());

                        break;
                    }
                }
            }
        }
    }
}
