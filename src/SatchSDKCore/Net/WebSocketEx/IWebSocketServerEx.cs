using System;
using System.Buffers;
using SSC.Net.HttpEx;

namespace SSC.Net.WebSocketEx;

public interface IWebSocketServerEx<TSession, TSessionID> : IHttpServerExRequestHandler, IDisposable
    where TSession : WebSocketServerExSession<TSession, TSessionID>
    where TSessionID : notnull
{
    string AbsolutePath { get; }
    int MaxReceiveQueueSize { get; }
    int MaxFrameLength { get; }
    int MaxMessageLength { get; }
    ArrayPool<byte> Allocator { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    void Start();
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
    void Stop();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Find a session by predicate
    /// </summary>
    /// <param name="predicate">Predicate to match the session</param>
    /// <param name="default">Default session to return if none matched</param>
    /// <returns>Matched session or default</returns>
    TSession? FindSession(Func<TSession, bool> predicate, TSession? @default = null);
}
