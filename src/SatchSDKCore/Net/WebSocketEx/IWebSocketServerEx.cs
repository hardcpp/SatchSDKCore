using SSC.Net.HttpEx;
using System;
using System.Buffers;

namespace SSC.Net.WebSocketEx;

public interface IWebSocketServerEx<TSession, TSessionID> : IHttpServerExRequestHandler, IDisposable
    where TSession   : WebSocketServerExSession<TSession, TSessionID>
    where TSessionID : notnull
{
    public string          AbsolutePath        { get; }
    public int             MaxReceiveQueueSize { get; }
    public int             MaxFrameLength      { get; }
    public int             MaxMessageLength    { get; }
    public ArrayPool<byte> Allocator           { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    public void Start();
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
    public void Stop();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Find a session by predicate
    /// </summary>
    /// <param name="predicate">Predicate to match the session</param>
    /// <param name="default">Default session to return if none matched</param>
    /// <returns>Matched session or default</returns>
    public TSession? FindSession(Func<TSession, bool> predicate, TSession? @default = null);
}
