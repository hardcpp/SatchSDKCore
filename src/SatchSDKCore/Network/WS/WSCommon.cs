using SSC.Misc;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Network.WS;

/// <summary>
/// Common class of WebSocket with utilities
/// </summary>
public abstract class WSCommon
{
    protected readonly SemaphoreSlim m_SendSemaphore      = new(1, 1);
    protected readonly SemaphoreSlim m_RecvQueueSemaphore = new(1, 1);

    protected readonly BlockingCollection<(byte[] data, int size, WebSocketMessageType messageType)>
        m_ReceivedMessages;

    protected CancellationTokenSource m_CancellationTokenSource = new();

    private readonly int m_MaxFrameLength;
    private readonly int m_MaxMessageLength;

    private bool   m_CloseSignaled     = false;
    private byte[] m_ReceiveBuffer     = null!;
    private int    m_ReceiveBufferWPos = 0;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly ArrayPool<byte> Allocator;

    public WebSocket?          Socket             { get; protected set; } = null!;
    public NameValueCollection NegociatingHeaders { get;  internal set; } = null!;
    public IPEndPoint          LocalEndPoint      { get;  internal set; } = null!;
    public IPEndPoint          RemoteEndPoint     { get;  internal set; } = null!;

    public bool IsConnected => Socket?.State == WebSocketState.Open;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="webSocket">WebSocket instance</param>
    /// <param name="allocator">Allocator, using shared if null</param>
    /// <param name="maxFrameLength">Max length of a single frame</param>
    /// <param name="maxMessageLength">Max length of a single message</param>
    /// <param name="maxReceiveQueueSize">Max length of the message queue</param>
    public WSCommon(
        WebSocket?       webSocket,
        ArrayPool<byte>? allocator           = null,
        int              maxFrameLength      = 1024,
        int              maxMessageLength    = 5 * 1024 * 1024,
        int              maxReceiveQueueSize = 50
        )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessageLength, maxFrameLength);

        Socket    = webSocket;
        Allocator = allocator ?? ArrayPool<byte>.Shared;

        m_ReceivedMessages = new BlockingCollection<(byte[], int, WebSocketMessageType)>(maxReceiveQueueSize);

        m_MaxFrameLength   = maxFrameLength;
        m_MaxMessageLength = maxMessageLength;

        m_ReceiveBuffer = new byte[m_MaxMessageLength];
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get origin IP address
    /// </summary>
    /// <returns>Origin IP address</returns>
    public IPAddress? GetRemoteIPAddress()
        => RemoteEndPoint?.Address;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On session open
    /// </summary>
    protected abstract void OnSocketOpen();
    /// <summary>
    /// On socket message
    /// </summary>
    /// <param name="data">Message data</param>
    /// <param name="type">Message type</param>
    protected abstract void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type);
    /// <summary>
    /// On socket close
    /// </summary>
    /// <param name="closeStatus">Close status</param>
    /// <param name="reason">Close reason</param>
    /// <param name="fromRemote">Is the closure originated from remote?</param>
    protected abstract void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// InternalStartRead from the socket
    /// </summary>
    protected async void InternalStartRead()
    {
        /// Give back execution immediatly to the caller
        await Task.Yield();

        while (true)
        {
            /// Stop reading if the socket is not connected
            if (!IsConnected || Socket == null || m_CancellationTokenSource.IsCancellationRequested)
                break;

            try
            {
                var l_IdealSize           = Math.Min(m_MaxFrameLength, m_ReceiveBuffer.Length - m_ReceiveBufferWPos);
                var l_ReceiveArraySegment = new ArraySegment<byte>(m_ReceiveBuffer, m_ReceiveBufferWPos, l_IdealSize);
                var l_Received            = await Socket.ReceiveAsync(l_ReceiveArraySegment, m_CancellationTokenSource.Token).ConfigureAwait(false);

                if (l_Received.MessageType == WebSocketMessageType.Binary || l_Received.MessageType == WebSocketMessageType.Text)
                {
                    m_ReceiveBufferWPos += l_Received.Count;

                    if (l_Received.EndOfMessage)
                    {
                        if (!m_CloseSignaled)
                        {
                            var l_Array = Allocator.Rent(m_ReceiveBufferWPos);
                            try
                            {
                                m_ReceiveBuffer.AsMemory(0, m_ReceiveBufferWPos).CopyTo(l_Array);

                                m_ReceivedMessages.Add((l_Array, m_ReceiveBufferWPos, l_Received.MessageType));
                            }
                            catch (Exception exception)
                            {
                                Allocator.Return(l_Array);

                                Logging.Log(
                                    ELogSeverity.Error,
                                    $"[Network.WebSocket][WebSocketSession.ReadSocket] Error while updating the session:"
                                );
                                Logging.Log(ELogSeverity.Error, exception);

                                Close(WebSocketCloseStatus.InternalServerError, "Internal error", sendClosure: true, fromRemote: false);

                                break;
                            }
                        }

                        m_ReceiveBufferWPos = 0;

                        /// Continue reading
                        continue;
                    }
                    /// Overflow detection
                    else if ((m_ReceiveBuffer.Length - m_ReceiveBufferWPos) == 0)
                    {
                        Logging.Log(
                            ELogSeverity.Error,
                            "[Network.WebSocket][WebSocketSession.ReadSocket] Message size overflow, closing socket..."
                        );

                        Close(WebSocketCloseStatus.MessageTooBig, "Message too big", sendClosure: true, fromRemote: false);
                        break;
                    }
                }
                else if (l_Received.MessageType == WebSocketMessageType.Close)
                {
                    Close(l_Received.CloseStatus, l_Received.CloseStatusDescription, sendClosure: false);
                    break;
                }
            }
            catch (Exception exception)
            {
                if (!m_CloseSignaled)
                {
                    Logging.Log(
                        ELogSeverity.Error,
                        "[Network.WebSocket][WebSocketSession.ReadSocket] Failed to read from socket"
                    );
                    Logging.Log(ELogSeverity.Error, exception);
                }

                Close(WebSocketCloseStatus.ProtocolError, "Failed to read", sendClosure: true, fromRemote: false);
                break;
            }
        }
    }
    /// <summary>
    /// Send a text message
    /// </summary>
    /// <param name="data">Message to send</param>
    /// <param name="encoding">Encoding, default to utf8</param>
    /// <returns>True if sended</returns>
    protected bool Send(string data, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(data))
        {
            Logging.Log(
                ELogSeverity.Error,
                "[Network.WebSocket][WebSocketSession.Send] Trying to send null data"
            );
            return false;
        }

        if (encoding == null)
            encoding = Encoding.UTF8;

        var l_BytesSize = FastTextEncoding.GetByteCount(data, encoding);
        var l_Array     = Allocator.Rent(l_BytesSize);

        try
        {
            var l_FinalSize = FastTextEncoding.GetBytes(data, encoding, l_Array.AsSpan());
            return Send(l_Array.AsMemory(0, l_FinalSize), WebSocketMessageType.Text);
        }
        finally
        {
            Allocator.Return(l_Array);
        }
    }
    /// <summary>
    /// Send a raw message
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="offset">Start position</param>
    /// <param name="length">Length to send</param>
    /// <param name="type">Message type</param>
    /// <returns>True if sended</returns>
    protected bool Send(byte[] data, int offset = 0, int length = -1, WebSocketMessageType type = WebSocketMessageType.Binary)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data == null)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.Send] Trying to send null data on websocket {RemoteEndPoint}"
            );
            return false;
        }

        var l_FixedLength = length != -1 ? length : data!.Length;
        return Send(new ReadOnlyMemory<byte>(data, offset, l_FixedLength), type);
    }
    /// <summary>
    /// Send a raw message
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="type">Message type</param>
    /// <returns>True if sended</returns>
    protected bool Send(ReadOnlyMemory<byte> data, WebSocketMessageType type = WebSocketMessageType.Binary)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(data.Length, 0);

        /// Stop sending if the socket is not connected
        if (!IsConnected || Socket == null)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.Send] Trying to send data on a closed websocket {RemoteEndPoint}"
            );
            return false;
        }

        if (!MemoryMarshal.TryGetArray(data, out var l_ArraySegment))
        {
            Logging.Log(
                ELogSeverity.Error,
                "[Network.WebSocket][WebSocketSession.ReadSocket] Failed to fetch the data to send"
            );
            return false;
        }

        var l_SkipRelease = false;
        try
        {
            m_SendSemaphore.Wait();

            var l_Sent      = 0;
            var l_Remaining = l_ArraySegment.Count;

            while (l_Remaining > 0)
            {
                var l_ToSend = Math.Min(l_Remaining, m_MaxFrameLength);
                var l_IsLast = l_ToSend == l_Remaining;

                var l_Task = Socket.SendAsync(l_ArraySegment.Slice(l_Sent, l_ToSend), type, l_IsLast, m_CancellationTokenSource.Token);
                l_Task.ConfigureAwait(false);
                l_Task.Wait();

                l_Sent      += l_ToSend;
                l_Remaining -= l_ToSend;
            }

            return true;
        }
        catch (Exception exception)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.Send] Error while writting on websocket {RemoteEndPoint}"
            );
            Logging.Log(ELogSeverity.Error, exception);

            m_SendSemaphore.Release();
            l_SkipRelease = true;

            Close(WebSocketCloseStatus.ProtocolError, "Error while writting", sendClosure: true, fromRemote: false);

            return false;
        }
        finally
        {
            if (!l_SkipRelease)
                m_SendSemaphore.Release();
        }
    }
    /// <summary>
    /// Close
    /// </summary>
    /// <param name="closeStatus">Close status</param>
    /// <param name="reason">Close reason</param>
    /// <param name="sendClosure">Send closure?</param>
    /// <param name="fromRemote">Is the closure originated from remote?</param>
    public void Close(WebSocketCloseStatus? closeStatus, string? reason, bool sendClosure, bool fromRemote = false)
    {
        if (m_CloseSignaled)
            return;

        m_CloseSignaled = true;

        if (!m_CancellationTokenSource.IsCancellationRequested)
            m_CancellationTokenSource.Cancel();

        if (sendClosure && Socket != null && IsConnected)
        {
            m_SendSemaphore.Wait();

            try
            {
                var l_Task = Socket.CloseOutputAsync(closeStatus ?? WebSocketCloseStatus.NormalClosure, reason ?? string.Empty, CancellationToken.None);
                l_Task.ConfigureAwait(false);
                l_Task.Wait();
            }
            catch (Exception)
            {
                /// Do nothing
            }
            finally
            {
                m_SendSemaphore.Release();
            }
        }

        try
        {
            Logging.Log(
                ELogSeverity.Verbose,
                $"[Network.WebSocket][WebSocketSession.InternalOnSocketClose] Closing socket for {RemoteEndPoint} status: {closeStatus} reason: {reason}"
            );
            OnSocketClose(closeStatus, reason, fromRemote);

            Socket?.Dispose();
        }
        catch (Exception exception)
        {
            Logging.Log(
                ELogSeverity.Error,
                $"[Network.WebSocket][WebSocketSession.InternalOnSocketClose] Closing socket error:"
            );
            Logging.Log(ELogSeverity.Error, exception);
        }

        Socket = null;

        while (m_ReceivedMessages.TryTake(out var l_Message))
            Allocator.Return(l_Message.data);
    }
}
