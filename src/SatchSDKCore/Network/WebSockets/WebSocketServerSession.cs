using System;
using System.Net.WebSockets;

namespace SSC.Network.WebSockets;

/// <summary>
/// WebSocketServer session
/// </summary>
public abstract class WebSocketServerSession : WebSocketCommon
{
    public readonly WebSocketServer Server;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="webSocketServer">WebSocketServer instance</param>
    /// <param name="webSocket">WebSocket instance</param>
    public WebSocketServerSession(WebSocketServer webSocketServer, WebSocket webSocket)
        : base(webSocket, webSocketServer.Allocator, webSocketServer.MaxFrameLength, webSocketServer.MaxMessageLength)
    {
        ArgumentNullException.ThrowIfNull(webSocketServer);
        ArgumentNullException.ThrowIfNull(webSocket);

        Server = webSocketServer;
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


    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On session open
    /// </summary>
    internal virtual void InternalOnSessionOpen()
    {
        /// Start reading
        InternalStartRead();

        OnSessionOpen();
        OnSocketOpen();
    }
    /// <summary>
    /// On session update
    /// </summary>
    internal virtual void InternalOnSessionUpdate()
    {
        byte[]? l_LastMsgBytes = null;
        while (m_ReceivedMessages.Count > 0)
        {
            try
            {
                m_RecvQueueSemaphore.Wait();
                var l_Message = m_ReceivedMessages.Dequeue();
                l_LastMsgBytes = l_Message.data;
                m_RecvQueueSemaphore.Release();

                OnSocketMessage(l_Message.data, l_Message.messageType);
            }
            catch (Exception exception)
            {
                m_RecvQueueSemaphore.Release();

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
                if (l_LastMsgBytes != null)
                {
                    Allocator.Return(l_LastMsgBytes);
                    l_LastMsgBytes = null;
                }
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
        OnSessionRemove();
    }
}
