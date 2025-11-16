using SSC.Net.HttpEx;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.WS;

/// <summary>
/// WebSocket server class
/// </summary>
public class WSServer<TSession,  TSessionID> : IHttpServerExRequestHandler
    where TSession   : WSServerSession<TSession, TSessionID>
    where TSessionID : notnull
{
    public delegate TSession d_MakeSession(WSServer<TSession, TSessionID> server, WebSocket webSocket);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly d_MakeSession                              _sessionFactory;
    private readonly List<TSession>                             _sessions = new(100);
    private readonly Thread[]                                   _workers;
    private readonly List<TSession>[]                           _workerSessions;
    private readonly ConcurrentQueue<TSession>[]                _workerNewSessions;

    private bool _isRunning = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HttpServerEx    HTTPServer;
    public readonly string          AbsolutePath;
    public readonly int             MaxReceiveQueueSize;
    public readonly int             MaxFrameLength;
    public readonly int             MaxMessageLength;
    public readonly ArrayPool<byte> Allocator;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpServer">HttpServerEx instance</param>
    /// <param name="absolutePath">URL absolutePath</param>
    /// <param name="makeSession">Session factory</param>
    /// <param name="workerCount">Worker count</param>
    /// <param name="minConcurentSessions">Minimum concurent sessions for memory allocation</param>
    /// <param name="maxReceiveQueueSize">Max size of the message queue for a session</param>
    /// <param name="maxFrameLength">Message frame length in bytes</param>
    /// <param name="maxMessageLength">Max message length in bytes</param>
    public WSServer(
        HttpServerEx    httpServer,
        string          absolutePath,
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

        if (!absolutePath.StartsWith("/"))
            throw new UriFormatException("Absolute path need to start with '/'");

        HTTPServer = httpServer;
        HTTPServer.AddRequestHandler(this);

        AbsolutePath        = absolutePath;
        MaxReceiveQueueSize = maxReceiveQueueSize;
        MaxFrameLength      = maxFrameLength;
        MaxMessageLength    = maxMessageLength;
        Allocator           = ArrayPool<byte>.Create(maxMessageLength, minConcurentSessions * maxReceiveQueueSize);

        _sessionFactory = makeSession;

        _workers           = new Thread[workerCount];
        _workerSessions    = new List<TSession>[workerCount];
        _workerNewSessions = new ConcurrentQueue<TSession>[workerCount];
        for (int i = 0; i < _workers.Length; i++)
        {
            var workerID = i;

            _workers[i]      = new Thread(() => WorkerLoop(workerID));
            _workers[i].Name = $"WebSocketServer {this.GetHashCode()} Worker #{i + 1}";

            _workerSessions[i]    = new List<TSession>(100);
            _workerNewSessions[i] = new ConcurrentQueue<TSession>();
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    public void Start()
    {
        _isRunning = true;

        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Start();
    }
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
    public void Stop()
    {
        _isRunning = false;

        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Join();

        _sessions.Clear();

        /// Close remaining queued session
        for (int i = 0; i < _workers.Length; i++)
        {
            while (_workerNewSessions[i].TryDequeue(out var newSession))
            {
                try
                {
                    if (newSession.IsConnected)
                        newSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);

                    newSession.InternalOnSessionRemove();
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
    public TSession? FindSession(Func<TSession, bool> predicate, TSession? @default = null)
    {
        lock (_sessions)
        {
            var linqResult = _sessions.FirstOrDefault(predicate);
            if (linqResult != null)
                return linqResult;
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
        var sessions    = _workerSessions[workerID];
        var newSessions = _workerNewSessions[workerID];

        while (_isRunning)
        {
            /// Look for new session to queue
            if (newSessions.TryDequeue(out var newSession))
            {
                newSession.InternalOnSessionOpen();
                lock (_sessions)
                    _sessions.Add(newSession);

                sessions.Add(newSession);
            }

            /// Update all sessions
            for (var i = 0; i < sessions.Count; ++i)
            {
                var currentSession = sessions[i];

                /// Look if the session is invalid/expired
                if (!currentSession.IsConnected)
                {
                    lock (_sessions)
                        _sessions.Remove(currentSession);

                    sessions.RemoveAt(i);
                    i--;

                    currentSession.Close(WebSocketCloseStatus.EndpointUnavailable, "Lost connection", sendClosure: false, fromRemote: false);
                    currentSession.InternalOnSessionRemove();

                    continue;
                }

                currentSession.InternalOnSessionUpdate();
            }

            if (sessions.Count > 10)
                Thread.Yield();
            else
                Thread.Sleep(1);
        }

        /// Close all sessions
        for (var i = 0; i < sessions.Count; ++i)
        {
            var currentSession = sessions[i];

            if (currentSession.IsConnected)
                currentSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", sendClosure: true, fromRemote: false);

            currentSession.InternalOnSessionRemove();
        }

        sessions.Clear();

        /// Pending new session cleaning is handled by the Stop method
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    protected override bool TryHandleImplementation(HttpServerExRequestContext context)
    {
        if (!_isRunning)
            return false;

        ArgumentNullException.ThrowIfNull(context);

        if (context.ListenerRequest.HttpMethod != "GET"
            || !context.ListenerRequest.IsWebSocketRequest
            || context.ListenerRequest.Url?.AbsolutePath != AbsolutePath)
            return false;

        var acceptTask = context.ListenerContext.AcceptWebSocketAsync(null, TimeSpan.FromSeconds(5));
        acceptTask.Wait();

        if (acceptTask.Status != TaskStatus.RanToCompletion)
        {
            Logging.Log(ELogSeverity.Error, "Failed to accept websocket request");
            if (acceptTask.Exception != null)
                Logging.Log(ELogSeverity.Error, acceptTask.Exception);

            return false;
        }

        var webSocketContext = acceptTask.Result;
        var webSocket        = webSocketContext.WebSocket;
        if (webSocket.State != WebSocketState.Open)
            return false;

        context.ConnectionUpgraded = true;

        var webSocketServerSession = _sessionFactory.Invoke(this, webSocket);
        webSocketServerSession.NegociatingHeaders = webSocketContext.Headers;
        webSocketServerSession.LocalEndPoint      = context.ListenerRequest.LocalEndPoint;
        webSocketServerSession.RemoteEndPoint     = context.ListenerRequest.RemoteEndPoint;

        var leastBusyWorker = Array.IndexOf(_workerSessions, _workerSessions.OrderBy((x) => x.Count).FirstOrDefault());
        _workerNewSessions[leastBusyWorker].Enqueue(webSocketServerSession);

        return true;
    }
}
