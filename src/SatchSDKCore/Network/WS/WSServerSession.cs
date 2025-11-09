using System;
using System.Net.WebSockets;

namespace SSC.Network.WS;

/// <summary>
/// WSServer session
/// </summary>
public abstract class WSServerSession<t_Session, t_SessionID> : WSCommon
    where t_Session : WSServerSession<t_Session, t_SessionID>
{
    public readonly WSServer<t_Session, t_SessionID>    Server;
    public readonly t_SessionID                         SessionID;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="server">WSServer instance</param>
    /// <param name="webSocket">WebSocket instance</param>
    public WSServerSession(WSServer<t_Session, t_SessionID> server, WebSocket webSocket, t_SessionID sessionID)
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
        while (m_ReceivedMessages.TryTake(out var l_Message))
        {
            var l_LastMsgBytes = l_Message.data;
            try
            {
                OnSocketMessage(l_Message.data.AsSpan(0, l_Message.size), l_Message.messageType);
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
                Allocator.Return(l_LastMsgBytes);
                l_LastMsgBytes = null;
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
