using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Network.WebSockets;

/// <summary>
/// WebSocket server class
/// </summary>
public class WebSocketServer : HTTP.IHTTPServerRequestHandler
{
    private readonly Thread[] m_Workers;
    private          bool     m_IsRunning = false;

    private Func<WebSocketServer, WebSocket, WebSocketServerSession>    m_SessionFactory;
    private ConcurrentQueue<WebSocketServerSession>                     m_SessionsToQueue = new ConcurrentQueue<WebSocketServerSession>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HTTP.HTTPServer HTTPServer;
    public readonly int             MaxFrameLength;
    public readonly int             MaxMessageLength;
    public readonly ArrayPool<byte> Allocator;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpServer">HTTPServer instance</param>
    /// <param name="makeSession">Session factory</param>
    /// <param name="workerCount">Worker count</param>
    /// <param name="maxFrameLength">Message frame length in bytes</param>
    /// <param name="maxMessageLength">Max message length in bytes</param>
    public WebSocketServer(
        HTTP.HTTPServer                                          httpServer,
        Func<WebSocketServer, WebSocket, WebSocketServerSession> makeSession,
        int                                                      workerCount      = 4,
        int                                                      maxFrameLength   = 1024,
        int                                                      maxMessageLength = 5 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessageLength, maxFrameLength);

        ArgumentNullException.ThrowIfNull(httpServer);
        ArgumentNullException.ThrowIfNull(makeSession);

        m_SessionFactory = makeSession;

        HTTPServer = httpServer;
        HTTPServer.AddRequestHandler(this);

        MaxFrameLength   = maxFrameLength;
        MaxMessageLength = maxMessageLength;
        Allocator        = ArrayPool<byte>.Create(maxMessageLength, workerCount * 50);

        m_Workers = new Thread[workerCount];
        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
        {
            m_Workers[l_I]      = new Thread(WorkerLoop);
            m_Workers[l_I].Name = $"WebSocketServer {this.GetHashCode()} Worker #{l_I + 1}";
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HTTP server and threads
    /// </summary>
    public void Start()
    {
        m_IsRunning = true;

        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
            m_Workers[l_I].Start();
    }
    /// <summary>
    /// Stop the HTTP server and wait for all the threads to stop
    /// </summary>
    public void Stop()
    {
        m_IsRunning = false;

        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
            m_Workers[l_I].Join();

        /// Close remaining queued session
        while (m_SessionsToQueue.TryDequeue(out var l_NewSession))
        {
            if (!l_NewSession.IsConnected)
                continue;

            l_NewSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);
            l_NewSession.InternalOnSessionRemove();
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Worker thread loop
    /// </summary>
    private void WorkerLoop()
    {
        var l_Sessions = new List<WebSocketServerSession>(100);

        while (m_IsRunning)
        {
            /// Look for new session to queue
            if (m_SessionsToQueue.TryDequeue(out var l_NewSession))
            {
                l_Sessions.Add(l_NewSession);
                l_NewSession.InternalOnSessionOpen();
            }

            /// Update all sessions
            for (var l_I = 0; l_I < l_Sessions.Count; ++l_I)
            {
                var l_CurrentSession = l_Sessions[l_I];

                /// Look if the session is invalid/expired
                if (!l_CurrentSession.IsConnected)
                {
                    /// todo...

                    l_Sessions.RemoveAt(l_I);
                    l_I--;

                    l_CurrentSession.Close(WebSocketCloseStatus.EndpointUnavailable, "Lost connection", sendClosure: false, fromRemote: false);
                    l_CurrentSession.InternalOnSessionRemove();

                    continue;
                }

                l_CurrentSession.InternalOnSessionUpdate();
            }

            Thread.Sleep(1);
        }

        /// Close all sessions
        for (var l_I = 0; l_I < l_Sessions.Count; ++l_I)
        {
            var l_CurrentSession = l_Sessions[l_I];
            if (!l_CurrentSession.IsConnected)
                continue;

            l_CurrentSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);
            l_CurrentSession.InternalOnSessionRemove();
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    protected override bool TryHandleImplementation(HTTP.HTTPServerRequestContext context)
    {
        if (!m_IsRunning)
            return false;

        ArgumentNullException.ThrowIfNull(context);

        if (context.ListenerRequest.HttpMethod != "GET" || !context.ListenerRequest.IsWebSocketRequest)
            return false;

        var l_AcceptTask = context.ListenerContext.AcceptWebSocketAsync(null, TimeSpan.FromSeconds(5));
        l_AcceptTask.Wait();

        if (l_AcceptTask.Status != TaskStatus.RanToCompletion)
        {
            Logging.Log(ELogSeverity.Error, "Failed to accept websocket request");
            if (l_AcceptTask.Exception != null)
                Logging.Log(ELogSeverity.Error, l_AcceptTask.Exception);

            return false;
        }

        var l_WebSocketContext = l_AcceptTask.Result;
        var l_WebSocket        = l_WebSocketContext.WebSocket;
        if (l_WebSocket.State != WebSocketState.Open)
            return false;

        context.ConnectionUpgraded = true;

        var l_WebSocketServerSession = m_SessionFactory.Invoke(this, l_WebSocket);
        l_WebSocketServerSession.NegociatingHeaders = l_WebSocketContext.Headers;
        l_WebSocketServerSession.LocalEndPoint      = context.ListenerRequest.LocalEndPoint;
        l_WebSocketServerSession.RemoteEndPoint     = context.ListenerRequest.RemoteEndPoint;

        m_SessionsToQueue.Enqueue(l_WebSocketServerSession);

        return true;
    }
}
