using System;
using System.Net.WebSockets;

namespace SSC.Net.WS;

/// <summary>
/// WSServer session
/// </summary>
public abstract class WSServerSession<TSession, TSessionID> : WSCommon
    where TSession   : WSServerSession<TSession, TSessionID>
    where TSessionID : notnull
{
    public readonly WSServer<TSession, TSessionID> Server;
    public readonly TSessionID                     SessionID;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="server">WSServer instance</param>
    /// <param name="webSocket">WebSocket instance</param>
    public WSServerSession(WSServer<TSession, TSessionID> server, WebSocket webSocket, TSessionID sessionID)
        : base(webSocket, server.Allocator, server.MaxFrameLength, server.MaxMessageLength, server.MaxReceiveQueueSize)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(webSocket);

        Server    = server;
        SessionID = sessionID;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On session open
    /// </summary>
    protected abstract void OnSessionOpen();
    /// <summary>
    /// On session update
    /// </summary>
    protected abstract void OnSessionUpdate();
    /// <summary>
    /// When the session is removed
    /// </summary>
    protected abstract void OnSessionRemove();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On session open
    /// </summary>
    internal virtual void InternalOnSessionOpen()
    {
        /// Start reading (Not waiting)
        InternalStartRead();

        try
        {
            OnSessionOpen();
            OnSocketOpen();
        }
        catch (Exception exception)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.InternalOnSessionOpen] Error while starting the session:"
            );
            Logging.Log(ELogSeverity.Error, exception);
        }
    }
    /// <summary>
    /// On session update
    /// </summary>
    internal virtual void InternalOnSessionUpdate()
    {
        while (_receivedMessages.TryTake(out var message))
        {
            var lastMsgBytes = message.data;
            try
            {
                OnSocketMessage(message.data.AsSpan(0, message.size), message.messageType);
            }
            catch (Exception exception)
            {
                Logging.Log(
                    ELogSeverity.Error,
                    "[Network.WebSocket][WebSocketSession.InternalOnSessionUpdate] Error while handling message:"
                );
                Logging.Log(ELogSeverity.Error, exception);

                break;
            }
            finally
            {
                /// Return bytes to allocator
                Allocator.Return(lastMsgBytes);
                lastMsgBytes = null;
            }
        }

        try
        {
            OnSessionUpdate();
        }
        catch (Exception exception)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.InternalOnSessionUpdate] Error while updating the session:"
            );
            Logging.Log(ELogSeverity.Error, exception);
        }
    }
    /// <summary>
    /// When the session is removed
    /// </summary>
    internal virtual void InternalOnSessionRemove()
    {
        try
        {
            OnSessionRemove();
        }
        catch (Exception exception)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.InternalOnSessionRemove] Error while terminating the session:"
            );
            Logging.Log(ELogSeverity.Error, exception);
        }
    }
}
