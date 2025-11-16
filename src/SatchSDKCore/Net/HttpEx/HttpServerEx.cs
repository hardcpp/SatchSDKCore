using SSC;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Server class
/// </summary>
public class HttpServerEx
{
    private static HttpServerExResponse s_Server404NotFoundResponse
        = new(HttpStatusCode.NotFound, new StringContent("404 Not found", Encoding.UTF8), Encoding.UTF8);
    private static HttpServerExResponse s_Server500InternalServerErrorResponse
        = new(HttpStatusCode.InternalServerError, new StringContent("500 Internal server error", Encoding.UTF8), Encoding.UTF8);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly HttpListener                         _listener;
    private readonly Thread                               _listenerThread;
    private readonly Thread[]                             _workers;
    private readonly ConcurrentQueue<HttpListenerContext> _contextQueue;
    private readonly ManualResetEvent                     _contextQueueEvent;

    private IHttpServerExRequestHook[]    _earlyHooks = Array.Empty<IHttpServerExRequestHook>();
    private IHttpServerExRequestHandler[] _handlers   = Array.Empty<IHttpServerExRequestHandler>();
    private IHttpServerExRequestHook[]    _lateHooks  = Array.Empty<IHttpServerExRequestHook>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public HttpListener listener => _listener;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefix">Listenning prefix</param>
    /// <param name="workerCount">Worker count</param>
    public HttpServerEx(string prefix, int workerCount = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        _listenerThread = new Thread(ListenerLoop);

        _workers = new Thread[workerCount];
        for (int l_I = 0; l_I < _workers.Length; l_I++)
        {
            _workers[l_I]      = new Thread(WorkerLoop);
            _workers[l_I].Name = $"HTTPServer {GetHashCode()} Worker #{l_I + 1}";
        }

        _contextQueue      = new ConcurrentQueue<HttpListenerContext>();
        _contextQueueEvent = new ManualResetEvent(false);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to add</param>
    public void AddEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var existingIdx = Array.IndexOf(_earlyHooks, earlyHook);
        if (existingIdx != -1)
            return;

        var newEarlyHooks = new IHttpServerExRequestHook[_earlyHooks.Length + 1];
        Array.Copy(_earlyHooks, newEarlyHooks, _earlyHooks.Length);
        newEarlyHooks[^1] = earlyHook;

        _earlyHooks = newEarlyHooks;
    }
    /// <summary>
    /// Remove a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to remove</param>
    public void RemoveEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var oldEarlyHooks = _earlyHooks;
        var existingIdx = Array.IndexOf(oldEarlyHooks, earlyHook);
        if (existingIdx == -1)
            return;

        var newEarlyHooks = new IHttpServerExRequestHook[oldEarlyHooks.Length - 1];
        Array.Copy(oldEarlyHooks,               0, newEarlyHooks,           0,                            existingIdx);
        Array.Copy(oldEarlyHooks, existingIdx + 1, newEarlyHooks, existingIdx, oldEarlyHooks.Length - existingIdx - 1);

        _earlyHooks = newEarlyHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a request handler
    /// </summary>
    /// <param name="handler">Handler to add</param>
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
    /// <summary>
    /// Remove a request handler
    /// </summary>
    /// <param name="handler">Handler to remove</param>
    public void RemoveRequestHandler(IHttpServerExRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var oldHandlers = _handlers;
        var existingIdx = Array.IndexOf(oldHandlers, handler);
        if (existingIdx == -1)
            return;

        var newHandlers = new IHttpServerExRequestHandler[oldHandlers.Length - 1];
        Array.Copy(oldHandlers,               0, newHandlers,           0,                          existingIdx);
        Array.Copy(oldHandlers, existingIdx + 1, newHandlers, existingIdx, oldHandlers.Length - existingIdx - 1);

        _handlers = newHandlers;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to add</param>
    public void AddLateRequestHook(IHttpServerExRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var existingIdx = Array.IndexOf(_lateHooks, lateHook);
        if (existingIdx != -1)
            return;

        var newLateHooks = new IHttpServerExRequestHook[_lateHooks.Length + 1];
        Array.Copy(_lateHooks, newLateHooks, _lateHooks.Length);
        newLateHooks[^1] = lateHook;

        _lateHooks = newLateHooks;
    }
    /// <summary>
    /// Remove a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to remove</param>
    public void RemoveLateRequestHook(IHttpServerExRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var oldLateHooks = _lateHooks;
        var existingIdx = Array.IndexOf(oldLateHooks, lateHook);
        if (existingIdx == -1)
            return;

        var newLateHooks = new IHttpServerExRequestHook[oldLateHooks.Length - 1];
        Array.Copy(oldLateHooks,               0, newLateHooks,           0,                           existingIdx);
        Array.Copy(oldLateHooks, existingIdx + 1, newLateHooks, existingIdx, oldLateHooks.Length - existingIdx - 1);

        _lateHooks = newLateHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    public void Start()
    {
        _listener.Start();
        _listenerThread.Start();

        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Start();
    }
    /// <summary>
    /// Wait for the server
    /// </summary>
    public void Wait()
    {
        if (!_listener.IsListening)
            return;

        _listenerThread.Join();
    }
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
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

            if (_contextQueue.TryDequeue(out HttpListenerContext? context) && context != null)
                HandleRequest(context);

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

            var earlyHooks = _earlyHooks;
            for (var i = 0; i < earlyHooks.Length; i++)
            {
                if (!earlyHooks[i].TryIntercept(serverContext))
                    continue;

                return;
            }

            var handlers   = _handlers;
            var wasHandled = false;
            for (var i = 0; i < handlers.Length; i++)
            {
                if (!handlers[i].TryHandle(serverContext))
                    continue;

                wasHandled = true;
                break;
            }

            var lateHooks = _lateHooks;
            for (var l_I = 0; l_I < lateHooks.Length; l_I++)
            {
                if (!lateHooks[l_I].TryIntercept(serverContext))
                    continue;

                return;
            }

            if (!serverContext.ConnectionUpgraded)
            {
                try
                {
                    if (!wasHandled || serverContext.ServerResponse == null)
                        serverContext.ServerResponse = s_Server404NotFoundResponse;

                    if (!serverContext.ServerResponse.TryWrite(listenerContext.Response, out var l_Error))
                        Logging.Log(ELogSeverity.Error, $"Failed to write response for request '{listenerContext.Request!.Url!.AbsolutePath}': {l_Error}");

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
                    listenerContext.Response.Close();
            }
            catch { }
        }
    }
}