using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using SSC.Misc.Hookable;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Server implementation using dotnet core implementation
/// </summary>
public class HttpServerExCore : IHttpServerEx
{
    private static readonly HttpServerExResponse s_Server404NotFoundResponse
        = new(HttpStatusCode.NotFound, new StringContent("404 Not found", Encoding.UTF8), Encoding.UTF8);
    private static readonly HttpServerExResponse s_Server500InternalServerErrorResponse
        = new(HttpStatusCode.InternalServerError, new StringContent("500 Internal server error", Encoding.UTF8), Encoding.UTF8);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;
    private readonly Thread[] _workers;
    private readonly ConcurrentQueue<HttpListenerContext> _contextQueue;
    private readonly ManualResetEvent _contextQueueEvent;

    private IHttpServerExRequestHandler[] _handlers = Array.Empty<IHttpServerExRequestHandler>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public HttpListener Listener => _listener;

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefix">Listenning prefix</param>
    /// <param name="workerCount">Worker count</param>
    public HttpServerExCore(string prefix, int workerCount = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        _listenerThread = new Thread(ListenerLoop);

        _workers = new Thread[workerCount];
        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i] = new Thread(WorkerLoop);
            _workers[i].Name = $"HTTPServer {GetHashCode()} Worker #{i + 1}";
        }

        _contextQueue = new ConcurrentQueue<HttpListenerContext>();
        _contextQueueEvent = new ManualResetEvent(false);
    }
    /// <inheritdoc/>
    public void Dispose()
        => Stop();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc/>
    public void AddRequestHandler(IHttpServerExRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var existingIdx = Array.IndexOf(_handlers, handler);
        if (existingIdx != -1)
            return;

        var newHandlers = new IHttpServerExRequestHandler[_handlers.Length + 1];
        Array.Copy(_handlers, newHandlers, _handlers.Length);
        newHandlers[^1] = handler;

        _handlers = newHandlers;
    }
    /// <inheritdoc/>
    public void RemoveRequestHandler(IHttpServerExRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var oldHandlers = _handlers;
        var existingIdx = Array.IndexOf(oldHandlers, handler);
        if (existingIdx == -1)
            return;

        var newHandlers = new IHttpServerExRequestHandler[oldHandlers.Length - 1];
        Array.Copy(oldHandlers, 0, newHandlers, 0, existingIdx);
        Array.Copy(oldHandlers, existingIdx + 1, newHandlers, existingIdx, oldHandlers.Length - existingIdx - 1);

        _handlers = newHandlers;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc/>
    public void Start()
    {
        _listener.Start();
        _listenerThread.Start();

        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Start();
    }
    /// <inheritdoc/>
    public void Wait()
    {
        if (!_listener.IsListening)
            return;

        _listenerThread.Join();
    }
    /// <inheritdoc/>
    public void Stop()
    {
        if (!_listener.IsListening)
            return;

        _listener.Stop();
        _listener.Close();

        _listenerThread.Join();
        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Join();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Main listenning thread loop
    /// </summary>
    private void ListenerLoop()
    {
        var callback = new AsyncCallback(OnRequestReceived);
        while (_listener.IsListening)
        {
            var context = _listener.BeginGetContext(callback, null);
            context.AsyncWaitHandle.WaitOne();
        }
    }
    /// <summary>
    /// Worker thread loop
    /// </summary>
    private void WorkerLoop()
    {
        while (_listener.IsListening)
        {
            if (!_contextQueueEvent.WaitOne(TimeSpan.FromMilliseconds(250)))
                continue;

            // Process all available items in the queue before resetting the event
            while (_contextQueue.TryDequeue(out HttpListenerContext? context))
            {
                if (context != null)
                    HandleRequest(context);
            }

            // Reset only if queue is truly empty after draining it
            if (_contextQueue.IsEmpty)
                _contextQueueEvent.Reset();
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// When a request is received
    /// </summary>
    /// <param name="result">Async callback data</param>
    private void OnRequestReceived(IAsyncResult result)
    {
        if (!_listener.IsListening)
            return;

        _contextQueue.Enqueue(_listener.EndGetContext(result));
        _contextQueueEvent.Set();
    }
    /// <summary>
    /// Handle a single request, passing it to the first willing IHTTPRequestHandler
    /// </summary>
    /// <param name="listenerContext">Request context</param>
    private void HandleRequest(HttpListenerContext listenerContext)
    {
        var serverContext = null as HttpServerExRequestContext;
        try
        {
            serverContext = new HttpServerExRequestContext(listenerContext);
            Logging.Log(ELogSeverity.Verbose, $"Request received: {serverContext.ListenerRequest.HttpMethod} {serverContext.ListenerRequest.Url}");

            if (Hooks.InterceptEarly(serverContext))
                return;

            var handlers = _handlers;
            var wasHandled = false;
            for (var i = 0; i < handlers.Length; i++)
            {
                var currentHandler = handlers[i];
                if (currentHandler.Hooks.InterceptEarly(serverContext))
                {
                    wasHandled = true;
                    break;
                }

                var handleResult = currentHandler.TryHandle(serverContext);

                // Make sure to run late hooks before final result
                if (currentHandler.Hooks.InterceptLate(serverContext)
                    || handleResult)
                {
                    wasHandled = true;
                    break;
                }
            }

            if (Hooks.InterceptLate(serverContext))
                return;

            if (!serverContext.ConnectionUpgraded)
            {
                try
                {
                    if (!wasHandled || serverContext.ServerResponse == null)
                        serverContext.ServerResponse = s_Server404NotFoundResponse;

                    if (!serverContext.ServerResponse.TryWrite(listenerContext.Response, out var writeError))
                        Logging.Log(ELogSeverity.Error, $"Failed to write response for request '{listenerContext.Request!.Url!.AbsolutePath}': {writeError}");

                    listenerContext.Response.OutputStream.Flush();
                    listenerContext.Response.Close();
                }
                catch { }
            }
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, exception);

            try
            {
                s_Server500InternalServerErrorResponse.TryWrite(listenerContext.Response, out _);
            }
            catch { }
        }
        finally
        {
            try
            {
                if (serverContext == null || !serverContext.ConnectionUpgraded)
                {
                    listenerContext.Response.OutputStream.Flush();
                    listenerContext.Response.Close();
                }
            }
            catch { }
        }
    }
}
