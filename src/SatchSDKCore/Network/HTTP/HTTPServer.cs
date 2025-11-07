using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace SSC.Network.HTTP;

/// <summary>
/// HTTP Server class
/// </summary>
public class HTTPServer
{
    private static HTTPServerResponse s_Server404NotFoundResponse
        = new(HttpStatusCode.NotFound, new StringContent("404 Not found", Encoding.UTF8), Encoding.UTF8);
    private static HTTPServerResponse s_Server500InternalServerErrorResponse
        = new(HttpStatusCode.InternalServerError, new StringContent("500 Internal server error", Encoding.UTF8), Encoding.UTF8);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly HttpListener                         m_Listener;
    private readonly Thread                               m_ListenerThread;
    private readonly Thread[]                             m_Workers;
    private readonly ConcurrentQueue<HttpListenerContext> m_ContextQueue;
    private readonly ManualResetEvent                     m_ContextQueueEvent;

    private IHTTPServerRequestHook[]    m_EarlyHooks = Array.Empty<IHTTPServerRequestHook>();
    private IHTTPServerRequestHandler[] m_Handlers   = Array.Empty<IHTTPServerRequestHandler>();
    private IHTTPServerRequestHook[]    m_LateHooks  = Array.Empty<IHTTPServerRequestHook>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public HttpListener listener => m_Listener;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefix">Listenning prefix</param>
    /// <param name="workerCount">Worker count</param>
    public HTTPServer(string prefix, int workerCount = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);

        m_Listener = new HttpListener();
        m_Listener.Prefixes.Add(prefix);

        m_ListenerThread = new Thread(ListenerLoop);

        m_Workers = new Thread[workerCount];
        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
            m_Workers[l_I] = new Thread(WorkerLoop);

        m_ContextQueue      = new ConcurrentQueue<HttpListenerContext>();
        m_ContextQueueEvent = new ManualResetEvent(false);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to add</param>
    public void AddEarlyRequestHook(IHTTPServerRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var l_ExistingIdx = Array.IndexOf(m_EarlyHooks, earlyHook);
        if (l_ExistingIdx != -1)
            return;

        var l_NewEarlyHooks = new IHTTPServerRequestHook[m_EarlyHooks.Length + 1];
        Array.Copy(m_EarlyHooks, l_NewEarlyHooks, m_EarlyHooks.Length);
        l_NewEarlyHooks[^1] = earlyHook;

        m_EarlyHooks = l_NewEarlyHooks;
    }
    /// <summary>
    /// Remove a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to remove</param>
    public void RemoveEarlyRequestHook(IHTTPServerRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var l_OldEarlyHooks = m_EarlyHooks;
        var l_ExistingIdx = Array.IndexOf(l_OldEarlyHooks, earlyHook);
        if (l_ExistingIdx == -1)
            return;

        var l_NewEarlyHooks = new IHTTPServerRequestHook[l_OldEarlyHooks.Length - 1];
        Array.Copy(l_OldEarlyHooks,                 0, l_NewEarlyHooks,             0,                                l_ExistingIdx);
        Array.Copy(l_OldEarlyHooks, l_ExistingIdx + 1, l_NewEarlyHooks, l_ExistingIdx, (l_OldEarlyHooks.Length - l_ExistingIdx) - 1);

        m_EarlyHooks = l_NewEarlyHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a request handler
    /// </summary>
    /// <param name="handler">Handler to add</param>
    public void AddRequestHandler(IHTTPServerRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var l_ExistingIdx = Array.IndexOf(m_Handlers, handler);
        if (l_ExistingIdx != -1)
            return;

        var l_NewHandlers = new IHTTPServerRequestHandler[m_Handlers.Length + 1];
        Array.Copy(m_Handlers, l_NewHandlers, m_Handlers.Length);
        l_NewHandlers[^1] = handler;

        m_Handlers = l_NewHandlers;
    }
    /// <summary>
    /// Remove a request handler
    /// </summary>
    /// <param name="handler">Handler to remove</param>
    public void RemoveRequestHandler(IHTTPServerRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var l_OldHandlers = m_Handlers;
        var l_ExistingIdx = Array.IndexOf(l_OldHandlers, handler);
        if (l_ExistingIdx == -1)
            return;

        var l_NewHandlers = new IHTTPServerRequestHandler[l_OldHandlers.Length - 1];
        Array.Copy(l_OldHandlers,                 0, l_NewHandlers,             0,                              l_ExistingIdx);
        Array.Copy(l_OldHandlers, l_ExistingIdx + 1, l_NewHandlers, l_ExistingIdx, (l_OldHandlers.Length - l_ExistingIdx) - 1);

        m_Handlers = l_NewHandlers;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to add</param>
    public void AddLateRequestHook(IHTTPServerRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var l_ExistingIdx = Array.IndexOf(m_LateHooks, lateHook);
        if (l_ExistingIdx != -1)
            return;

        var l_NewLateHooks = new IHTTPServerRequestHook[m_LateHooks.Length + 1];
        Array.Copy(m_LateHooks, l_NewLateHooks, m_LateHooks.Length);
        l_NewLateHooks[^1] = lateHook;

        m_LateHooks = l_NewLateHooks;
    }
    /// <summary>
    /// Remove a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to remove</param>
    public void RemoveLateRequestHook(IHTTPServerRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var l_OldLateHooks = m_LateHooks;
        var l_ExistingIdx = Array.IndexOf(l_OldLateHooks, lateHook);
        if (l_ExistingIdx == -1)
            return;

        var l_NewLateHooks = new IHTTPServerRequestHook[l_OldLateHooks.Length - 1];
        Array.Copy(l_OldLateHooks,                 0, l_NewLateHooks,             0,                               l_ExistingIdx);
        Array.Copy(l_OldLateHooks, l_ExistingIdx + 1, l_NewLateHooks, l_ExistingIdx, (l_OldLateHooks.Length - l_ExistingIdx) - 1);

        m_LateHooks = l_NewLateHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HTTP server and threads
    /// </summary>
    public void Start()
    {
        m_Listener.Start();
        m_ListenerThread.Start();

        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
            m_Workers[l_I].Start();
    }
    /// <summary>
    /// Wait for the server
    /// </summary>
    public void Wait()
    {
        if (!m_Listener.IsListening)
            return;

        m_ListenerThread.Join();
    }
    /// <summary>
    /// Stop the HTTP server and wait for all the threads to stop
    /// </summary>
    public void Stop()
    {
        if (!m_Listener.IsListening)
            return;

        m_Listener.Stop();
        m_Listener.Close();

        m_ListenerThread.Join();
        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
            m_Workers[l_I].Join();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Main listenning thread loop
    /// </summary>
    private void ListenerLoop()
    {
        var l_Callback = new AsyncCallback(OnRequestReceived);
        while (m_Listener.IsListening)
        {
            var l_Context = m_Listener.BeginGetContext(l_Callback, null);
            l_Context.AsyncWaitHandle.WaitOne();
        }
    }
    /// <summary>
    /// Worker thread loop
    /// </summary>
    private void WorkerLoop()
    {
        while (m_Listener.IsListening)
        {
            if (!m_ContextQueueEvent.WaitOne(TimeSpan.FromMilliseconds(250)))
                continue;

            if (m_ContextQueue.TryDequeue(out HttpListenerContext? context) && context != null)
                HandleRequest(context);

            if (m_ContextQueue.IsEmpty)
                m_ContextQueueEvent.Reset();
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
        if (!m_Listener.IsListening)
            return;

        m_ContextQueue.Enqueue(m_Listener.EndGetContext(result));
        m_ContextQueueEvent.Set();
    }
    /// <summary>
    /// Handle a single request, passing it to the first willing IHTTPRequestHandler
    /// </summary>
    /// <param name="listenerContext">Request context</param>
    private void HandleRequest(HttpListenerContext listenerContext)
    {
        var l_ServerContext = null as HTTPServerRequestContext;
        try
        {
            l_ServerContext = new HTTPServerRequestContext(listenerContext);
            Logging.Log(ELogSeverity.Verbose, $"Request received: {l_ServerContext.ListenerRequest.HttpMethod} {l_ServerContext.ListenerRequest.Url}");

            var l_EarlyHooks = m_EarlyHooks;
            for (var l_I = 0; l_I < l_EarlyHooks.Length; l_I++)
            {
                if (!l_EarlyHooks[l_I].TryIntercept(l_ServerContext))
                    continue;

                return;
            }

            var l_Handlers   = m_Handlers;
            var l_WasHandled = false;
            for (var l_I = 0; l_I < l_Handlers.Length; l_I++)
            {
                if (!l_Handlers[l_I].TryHandle(l_ServerContext))
                    continue;

                l_WasHandled = true;
                break;
            }

            var l_LateHooks = m_LateHooks;
            for (var l_I = 0; l_I < l_LateHooks.Length; l_I++)
            {
                if (!l_LateHooks[l_I].TryIntercept(l_ServerContext))
                    continue;

                return;
            }

            if (!l_ServerContext.ConnectionUpgraded)
            {
                try
                {
                    if (!l_WasHandled || l_ServerContext.ServerResponse == null)
                        l_ServerContext.ServerResponse = s_Server404NotFoundResponse;

                    if (!l_ServerContext.ServerResponse.TryWrite(listenerContext.Response, out var l_Error))
                        Logging.Log(ELogSeverity.Error, $"Failed to write response for request '{listenerContext.Request!.Url!.AbsolutePath}': {l_Error}");

                    listenerContext.Response.Close();
                }
                catch { }
            }
        }
        catch (Exception l_Exception)
        {
            Logging.Log(ELogSeverity.Error, l_Exception);

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
                if (l_ServerContext == null || !l_ServerContext.ConnectionUpgraded)
                    listenerContext.Response.Close();
            }
            catch { }
        }
    }
}