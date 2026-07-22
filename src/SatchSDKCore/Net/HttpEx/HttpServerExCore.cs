using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SSC.Misc;
using SSC.Misc.Hookable;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced HTTP server implementation using the .NET HttpListener.
/// </summary>
public class HttpServerExCore : IHttpServerEx
{
    private static readonly HttpServerExResponse s_Server404NotFoundResponse =
        new(HttpStatusCode.NotFound, new StringContent("404 Not found", Encoding.UTF8), Encoding.UTF8);

    private static readonly HttpServerExResponse s_Server500InternalServerErrorResponse =
        new(HttpStatusCode.InternalServerError, new StringContent("500 Internal server error", Encoding.UTF8),
            Encoding.UTF8);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly ConcurrentDictionary<long, Task> _activeRequests = new();
    private readonly object                           _configurationLock = new();
    private readonly SemaphoreSlim                    _requestSlots;
    private readonly CancellationTokenSource          _shutdownCancellation = new();
    private          IHttpServerExRequestHandler[]    _handlers = Array.Empty<IHttpServerExRequestHandler>();
    private          Task?                            _listenerTask;
    private          long                             _nextRequestId;
    private          int                              _started;
    private          int                              _stopping;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public HttpListener Listener { get; }

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefix">Listen prefix</param>
    /// <param name="maxConcurrentRequests">Maximum amount of parallel requests</param>
    public HttpServerExCore(string prefix, int maxConcurrentRequests = 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentRequests, 1);

        Listener = new HttpListener();
        Listener.Prefixes.Add(prefix);
        _requestSlots = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _requestSlots.Dispose();
        _shutdownCancellation.Dispose();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void AddRequestHandler(IHttpServerExRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_configurationLock)
        {
            if (Volatile.Read(ref _started) != 0)
                throw new InvalidOperationException("Request handlers are frozen after the server starts");
            if (Array.IndexOf(_handlers, handler) != -1)
                return;

            var newHandlers = new IHttpServerExRequestHandler[_handlers.Length + 1];
            Array.Copy(_handlers, newHandlers, _handlers.Length);
            newHandlers[^1] = handler;
            Volatile.Write(ref _handlers, newHandlers);
        }
    }

    /// <inheritdoc />
    public void RemoveRequestHandler(IHttpServerExRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_configurationLock)
        {
            if (Volatile.Read(ref _started) != 0)
                throw new InvalidOperationException("Request handlers are frozen after the server starts");
            IHttpServerExRequestHandler[] oldHandlers = _handlers;
            int                           existingIdx = Array.IndexOf(oldHandlers, handler);
            if (existingIdx == -1)
                return;

            var newHandlers = new IHttpServerExRequestHandler[oldHandlers.Length - 1];
            Array.Copy(oldHandlers, 0,               newHandlers, 0,           existingIdx);
            Array.Copy(oldHandlers, existingIdx + 1, newHandlers, existingIdx, oldHandlers.Length - existingIdx - 1);
            Volatile.Write(ref _handlers, newHandlers);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void Start()
    {
        lock (_configurationLock)
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                throw new InvalidOperationException("The HTTP server has already been started");

            foreach (IHttpServerExRequestHandler handler in _handlers)
            {
                if (handler is IFreezable freezableHandler)
                    freezableHandler.Freeze();
            }

            Listener.Start();
            _listenerTask = ListenerLoopAsync(_shutdownCancellation.Token);
        }
    }

    /// <inheritdoc />
    public void Wait()
        => WaitAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task WaitAsync()
    {
        Task? listenerTask = _listenerTask;
        if (listenerTask == null)
            return;

        await listenerTask.ConfigureAwait(false);

        while (!_activeRequests.IsEmpty)
        {
            Task[] requests = _activeRequests.Values.ToArray();
            if (requests.Length == 0)
                break;
            await Task.WhenAll(requests).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Stop()
        => StopAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask StopAsync()
    {
        if (Interlocked.Exchange(ref _stopping, 1) != 0)
        {
            await WaitAsync().ConfigureAwait(false);
            return;
        }

        _shutdownCancellation.Cancel();
        try
        {
            Listener.Stop();
        }
        catch (ObjectDisposedException)
        {
        }

        await WaitAsync().ConfigureAwait(false);
        Listener.Close();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private async Task ListenerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && Listener.IsListening)
            {
                HttpListenerContext listenerContext;
                try
                {
                    listenerContext = await Listener
                        .GetContextAsync()
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (HttpListenerException) when (!Listener.IsListening)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                try
                {
                    await _requestSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    AbortResponseContext(listenerContext);
                    throw;
                }

                long requestId = Interlocked.Increment(ref _nextRequestId);
                Task requestTask = Task.Run(
                    () => HandleRequestWithReleaseAsync(listenerContext, cancellationToken),
                    CancellationToken.None);
                _activeRequests[requestId] = requestTask;
                _                          = ObserveAndRemoveRequestAsync(requestId, requestTask);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, exception);
        }
    }

    private async Task HandleRequestWithReleaseAsync(
        HttpListenerContext listenerContext,
        CancellationToken   cancellationToken)
    {
        try
        {
            await HandleRequestAsync(listenerContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _requestSlots.Release();
        }
    }

    private async Task ObserveAndRemoveRequestAsync(long requestId, Task requestTask)
    {
        try
        {
            await requestTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, exception);
        }
        finally
        {
            _activeRequests.TryRemove(requestId, out _);
        }
    }

    private async Task HandleRequestAsync(
        HttpListenerContext listenerContext,
        CancellationToken   cancellationToken)
    {
        HttpServerExRequestContext? serverContext = null;
        try
        {
            serverContext = new HttpServerExRequestContext(listenerContext);
            Logging.Log(
                ELogSeverity.Verbose,
                $"Request received: {serverContext.ListenerRequest.HttpMethod} {serverContext.ListenerRequest.Url}");

            if (Hooks.InterceptEarly(serverContext))
                return;

            IHttpServerExRequestHandler[] handlers   = Volatile.Read(ref _handlers);
            bool                          wasHandled = false;
            for (int i = 0; i < handlers.Length; i++)
            {
                IHttpServerExRequestHandler currentHandler = handlers[i];
                if (currentHandler.Hooks.InterceptEarly(serverContext))
                {
                    wasHandled = true;
                    break;
                }

                bool handleResult = await currentHandler
                    .TryHandleAsync(serverContext, cancellationToken)
                    .ConfigureAwait(false);

                if (currentHandler.Hooks.InterceptLate(serverContext) || handleResult)
                {
                    wasHandled = true;
                    break;
                }
            }

            if (Hooks.InterceptLate(serverContext) || serverContext.ConnectionUpgraded)
                return;

            if (!wasHandled || serverContext.ServerResponse == null)
                serverContext.ServerResponse = s_Server404NotFoundResponse;

            await serverContext.ServerResponse
                .WriteAsync(listenerContext.Response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, exception);
            try
            {
                await s_Server500InternalServerErrorResponse
                    .WriteAsync(listenerContext.Response, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
        }
        finally
        {
            if (serverContext == null || !serverContext.ConnectionUpgraded)
            {
                try
                {
                    await listenerContext.Response.OutputStream
                        .FlushAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch
                {
                }

                CloseContext(listenerContext);
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private static void CloseContext(HttpListenerContext context)
    {
        try
        {
            context.Response.Close();
        }
        catch
        {
        }
    }

    private static void AbortResponseContext(HttpListenerContext context)
    {
        try
        {
            context.Response.Abort();
        }
        catch
        {
        }
    }
}
