using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Network.WS;


/// <summary>
/// WebSocket server class
/// </summary>
public class WSServer<t_Session,  t_SessionID> : HTTP.IHTTPServerRequestHandler
    where t_Session : WSServerSession<t_Session, t_SessionID>
{
    public delegate t_Session d_MakeSession(WSServer<t_Session, t_SessionID> server, WebSocket webSocket);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly Thread[]                     m_Workers;
    private readonly List<t_Session>[]            m_WorkerSessions;
    private readonly ConcurrentQueue<t_Session>[] m_WorkerNewSessions;

    private readonly List<t_Session>              m_Sessions;

    private          bool m_IsRunning = false;
    private d_MakeSession m_SessionFactory;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HTTP.HTTPServer HTTPServer;
    public readonly int             MaxReceiveQueueSize;
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
    /// <param name="minConcurentSessions">Minimum concurent sessions for memory allocation</param>
    /// <param name="maxReceiveQueueSize">Max size of the message queue for a session</param>
    /// <param name="maxFrameLength">Message frame length in bytes</param>
    /// <param name="maxMessageLength">Max message length in bytes</param>
    public WSServer(
        HTTP.HTTPServer httpServer,
        d_MakeSession   makeSession,
        int             workerCount          = 4,
        int             minConcurentSessions = 500,
        int             maxReceiveQueueSize  = 50,
        int             maxFrameLength       = 1024,
        int             maxMessageLength     = 5 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessageLength, maxFrameLength);

        ArgumentNullException.ThrowIfNull(httpServer);
        ArgumentNullException.ThrowIfNull(makeSession);

        m_SessionFactory = makeSession;

        HTTPServer = httpServer;
        HTTPServer.AddRequestHandler(this);

        MaxReceiveQueueSize = maxReceiveQueueSize;
        MaxFrameLength      = maxFrameLength;
        MaxMessageLength    = maxMessageLength;
        Allocator           = ArrayPool<byte>.Create(maxMessageLength, minConcurentSessions * maxReceiveQueueSize);

        m_Workers           = new Thread[workerCount];
        m_WorkerSessions    = new List<t_Session>[workerCount];
        m_WorkerNewSessions = new ConcurrentQueue<t_Session>[workerCount];
        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
        {
            var workerID = l_I;

            m_Workers[l_I]      = new Thread(() => WorkerLoop(workerID));
            m_Workers[l_I].Name = $"WebSocketServer {this.GetHashCode()} Worker #{l_I + 1}";

            m_WorkerSessions[l_I]    = new List<t_Session>(100);
            m_WorkerNewSessions[l_I] = new ConcurrentQueue<t_Session>();
        }

        m_Sessions = new List<t_Session>(100 * workerCount);
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

        m_Sessions.Clear();

        /// Close remaining queued session
        for (int l_I = 0; l_I < m_Workers.Length; l_I++)
        {
            while (m_WorkerNewSessions[l_I].TryDequeue(out var l_NewSession))
            {
                try
                {
                    if (l_NewSession.IsConnected)
                        l_NewSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);

                    l_NewSession.InternalOnSessionRemove();
                }
                catch (Exception)
                {
                    /// Do nothing
                }
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Find a session by predicate
    /// </summary>
    /// <param name="predicate">Predicate to match the session</param>
    /// <param name="default">Default session to return if none matched</param>
    /// <returns>Matched session or default</returns>
    public t_Session? FindSession(Func<t_Session, bool> predicate, t_Session? @default = null)
    {
        lock (m_Sessions)
        {
            var l_SubRes = m_Sessions.FirstOrDefault(predicate);
            if (l_SubRes != null)
                return l_SubRes;
        }

        return @default;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Worker thread loop
    /// </summary>
    private void WorkerLoop(int workerID)
    {
        var l_Sessions    = m_WorkerSessions[workerID];
        var l_NewSessions = m_WorkerNewSessions[workerID];

        while (m_IsRunning)
        {
            /// Look for new session to queue
            if (l_NewSessions.TryDequeue(out var l_NewSession))
            {
                l_NewSession.InternalOnSessionOpen();
                lock (m_Sessions)
                    m_Sessions.Add(l_NewSession);

                l_Sessions.Add(l_NewSession);
            }

            /// Update all sessions
            for (var l_I = 0; l_I < l_Sessions.Count; ++l_I)
            {
                var l_CurrentSession = l_Sessions[l_I];

                /// Look if the session is invalid/expired
                if (!l_CurrentSession.IsConnected)
                {
                    lock (m_Sessions)
                        m_Sessions.Remove(l_CurrentSession);

                    l_Sessions.RemoveAt(l_I);
                    l_I--;

                    l_CurrentSession.Close(WebSocketCloseStatus.EndpointUnavailable, "Lost connection", sendClosure: false, fromRemote: false);
                    l_CurrentSession.InternalOnSessionRemove();

                    continue;
                }

                l_CurrentSession.InternalOnSessionUpdate();
            }

            if (l_Sessions.Count > 10)
                Thread.Yield();
            else
                Thread.Sleep(1);
        }

        /// Close all sessions
        for (var l_I = 0; l_I < l_Sessions.Count; ++l_I)
        {
            var l_CurrentSession = l_Sessions[l_I];

            if (l_CurrentSession.IsConnected)
                l_CurrentSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);

            l_CurrentSession.InternalOnSessionRemove();
        }

        l_Sessions.Clear();

        /// Pending new session cleaning is handled by the Stop method
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

        var leastBusyWorker = Array.IndexOf(m_WorkerSessions, m_WorkerSessions.OrderBy((x) => x.Count).FirstOrDefault());
        m_WorkerNewSessions[leastBusyWorker].Enqueue(l_WebSocketServerSession);

        return true;
    }
}
