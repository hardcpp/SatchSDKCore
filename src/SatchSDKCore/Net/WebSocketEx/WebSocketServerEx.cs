using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Net.WebSocketEx;

/// <summary>
/// Advanced WebSocket server class
/// </summary>
public class WebSocketServerEx<TSession, TSessionID>
    : IWebSocketServerEx<TSession, TSessionID>,
      IHttpServerExRequestHandler, IDisposable
    where TSession : WebSocketServerExSession<TSession, TSessionID>
    where TSessionID : notnull
{
    public delegate TSession MakeSessionCallback(WebSocketServerEx<TSession, TSessionID> server, WebSocket webSocket);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly object                      _configurationLock = new();
    private readonly MakeSessionCallback         _sessionFactory;
    private readonly List<TSession>              _sessions = new(100);
    private readonly Thread[]                    _workers;
    private readonly List<TSession>[]            _workerSessions;
    private readonly ConcurrentQueue<TSession>[] _workerNewSessions;

    private bool _isRunning;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHttpServerEx   HttpServer          { get; init; }
    public string          AbsolutePath        { get; init; }
    public int             MaxReceiveQueueSize { get; init; }
    public int             MaxFrameLength      { get; init; }
    public int             MaxMessageLength    { get; init; }
    public ArrayPool<byte> Allocator           { get; init; }

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

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
    public WebSocketServerEx(
        IHttpServerEx       httpServer,
        string              absolutePath,
        MakeSessionCallback makeSession,
        int                 workerCount          = 4,
        int                 minConcurentSessions = 500,
        int                 maxReceiveQueueSize  = 50,
        int                 maxFrameLength       = 1024,
        int                 maxMessageLength     = 5 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount,      1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessageLength, maxFrameLength);

        ArgumentNullException.ThrowIfNull(httpServer);
        ArgumentNullException.ThrowIfNull(makeSession);

        if (!absolutePath.StartsWith("/"))
            throw new UriFormatException("Absolute path need to start with '/'");

        HttpServer = httpServer;

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
            int workerID = i;

            _workers[i]      = new Thread(() => WorkerLoop(workerID));
            _workers[i].Name = $"WebSocketServer {GetHashCode()} Worker #{i + 1}";

            _workerSessions[i]    = new List<TSession>(100);
            _workerNewSessions[i] = new ConcurrentQueue<TSession>();
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void Start()
    {
        lock (_configurationLock)
        {
            if (_isRunning)
                return;

            _isRunning = true;

            HttpServer.AddRequestHandler(this);

            for (int i = 0; i < _workers.Length; i++)
                _workers[i].Start();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_configurationLock)
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            HttpServer.RemoveRequestHandler(this);

            for (int i = 0; i < _workers.Length; i++)
                _workers[i].Join();

            _sessions.Clear();

            /// Close remaining queued session
            for (int i = 0; i < _workers.Length; i++)
            {
                while (_workerNewSessions[i].TryDequeue(out TSession? newSession))
                {
                    try
                    {
                        if (newSession.IsConnected)
                            newSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", true);

                        newSession.InternalOnSessionRemove();
                    }
                    catch (Exception)
                    {
                        /// Do nothing
                    }
                }
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public TSession? FindSession(Func<TSession, bool> predicate, TSession? @default = null)
    {
        lock (_sessions)
        {
            TSession? linqResult = _sessions.FirstOrDefault(predicate);
            if (linqResult != null)
                return linqResult;
        }

        return @default;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpServerExRequestContext context,
        CancellationToken          cancellationToken = default)
    {
        if (!_isRunning)
            return false;

        ArgumentNullException.ThrowIfNull(context);

        if (context.ListenerRequest.HttpMethod != "GET"
         || !context.ListenerRequest.IsWebSocketRequest
         || context.ListenerRequest.Url?.AbsolutePath != AbsolutePath)
            return false;

        WebSocketContext webSocketContext;
        try
        {
            webSocketContext = await context.ListenerContext
                .AcceptWebSocketAsync(null, TimeSpan.FromSeconds(5))
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, "Failed to accept websocket request");
            Logging.Log(ELogSeverity.Error, exception);
            return false;
        }

        WebSocket webSocket = webSocketContext.WebSocket;
        if (webSocket.State != WebSocketState.Open)
            return false;

        context.ConnectionUpgraded = true;

        TSession webSocketServerSession = _sessionFactory.Invoke(this, webSocket);
        webSocketServerSession.NegociatingHeaders = webSocketContext.Headers;
        webSocketServerSession.LocalEndPoint      = context.ListenerRequest.LocalEndPoint;
        webSocketServerSession.RemoteEndPoint     = context.ListenerRequest.RemoteEndPoint;

        int leastBusyWorker = Array.IndexOf(_workerSessions, _workerSessions.OrderBy(x => x.Count).FirstOrDefault());
        _workerNewSessions[leastBusyWorker].Enqueue(webSocketServerSession);

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Worker thread loop
    /// </summary>
    private void WorkerLoop(int workerID)
    {
        List<TSession>            sessions    = _workerSessions[workerID];
        ConcurrentQueue<TSession> newSessions = _workerNewSessions[workerID];

        while (_isRunning)
        {
            /// Look for new session to queue
            if (newSessions.TryDequeue(out TSession? newSession))
            {
                newSession.InternalOnSessionOpen();
                lock (_sessions)
                    _sessions.Add(newSession);

                sessions.Add(newSession);
            }

            /// Update all sessions
            for (int i = 0; i < sessions.Count; ++i)
            {
                TSession currentSession = sessions[i];

                /// Look if the session is invalid/expired
                if (!currentSession.IsConnected)
                {
                    lock (_sessions)
                        _sessions.Remove(currentSession);

                    sessions.RemoveAt(i);
                    i--;

                    currentSession.Close(WebSocketCloseStatus.EndpointUnavailable, "Lost connection", false);
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
        for (int i = 0; i < sessions.Count; ++i)
        {
            TSession currentSession = sessions[i];

            if (currentSession.IsConnected)
                currentSession.Close(WebSocketCloseStatus.NormalClosure, "Server stopping", true);

            currentSession.InternalOnSessionRemove();
        }

        sessions.Clear();

        /// Pending new session cleaning is handled by the Stop method
    }
}
