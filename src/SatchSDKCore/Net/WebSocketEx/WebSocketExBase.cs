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

namespace SSC.Net.WebSocketEx;

/// <summary>
/// Common class of advanced WebSocket with utilities
/// </summary>
public abstract class WebSocketExBase
{
    protected readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    protected readonly BlockingCollection<(byte[] data, int size, WebSocketMessageType messageType)>
        _receivedMessages;

    protected CancellationTokenSource _cancellationTokenSource = new();

    private readonly int _maxFrameLength;
    private readonly int _maxMessageLength;

    private bool   _closeSignaled     = false;
    private byte[] _receiveBuffer     = null!;
    private int    _receiveBufferWPos = 0;

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
    public WebSocketExBase(
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

        _receivedMessages = new BlockingCollection<(byte[], int, WebSocketMessageType)>(maxReceiveQueueSize);

        _maxFrameLength   = maxFrameLength;
        _maxMessageLength = maxMessageLength;

        _receiveBuffer = new byte[_maxMessageLength];
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
            if (!IsConnected || Socket == null || _cancellationTokenSource.IsCancellationRequested)
                break;

            try
            {
                var idealSize           = Math.Min(_maxFrameLength, _receiveBuffer.Length - _receiveBufferWPos);
                var receiveArraySegment = new ArraySegment<byte>(_receiveBuffer, _receiveBufferWPos, idealSize);
                var received            = await Socket.ReceiveAsync(receiveArraySegment, _cancellationTokenSource.Token).ConfigureAwait(false);

                if (received.MessageType == WebSocketMessageType.Binary || received.MessageType == WebSocketMessageType.Text)
                {
                    _receiveBufferWPos += received.Count;

                    if (received.EndOfMessage)
                    {
                        if (!_closeSignaled)
                        {
                            var array = Allocator.Rent(_receiveBufferWPos);
                            try
                            {
                                _receiveBuffer.AsMemory(0, _receiveBufferWPos).CopyTo(array);

                                _receivedMessages.Add((array, _receiveBufferWPos, received.MessageType));
                            }
                            catch (Exception exception)
                            {
                                Allocator.Return(array);

                                Logging.Log(
                                    ELogSeverity.Error,
                                    $"[Network.WebSocket][WebSocketSession.ReadSocket] Error while updating the session:"
                                );
                                Logging.Log(ELogSeverity.Error, exception);

                                Close(WebSocketCloseStatus.InternalServerError, "Internal error", sendClosure: true, fromRemote: false);

                                break;
                            }
                        }

                        _receiveBufferWPos = 0;

                        /// Continue reading
                        continue;
                    }
                    /// Overflow detection
                    else if ((_receiveBuffer.Length - _receiveBufferWPos) == 0)
                    {
                        Logging.Log(
                            ELogSeverity.Error,
                            "[Network.WebSocket][WebSocketSession.ReadSocket] Message size overflow, closing socket..."
                        );

                        Close(WebSocketCloseStatus.MessageTooBig, "Message too big", sendClosure: true, fromRemote: false);
                        break;
                    }
                }
                else if (received.MessageType == WebSocketMessageType.Close)
                {
                    Close(received.CloseStatus, received.CloseStatusDescription, sendClosure: false);
                    break;
                }
            }
            catch (Exception exception)
            {
                if (!_closeSignaled)
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
        ArgumentNullException.ThrowIfNullOrEmpty(data);

        if (encoding == null)
            encoding = Encoding.UTF8;

        var bytesSize = FastTextEncoding.GetByteCount(data, encoding);
        var array     = Allocator.Rent(bytesSize);

        try
        {
            var l_FinalSize = FastTextEncoding.GetBytes(data, encoding, array.AsSpan());
            return Send(array.AsMemory(0, l_FinalSize), WebSocketMessageType.Text);
        }
        finally
        {
            Allocator.Return(array);
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

        /// Get the ArraySegment to avoid potential copy in native send implementation
        if (!MemoryMarshal.TryGetArray(data, out var l_ArraySegment))
        {
            Logging.Log(
                ELogSeverity.Error,
                "[Network.WebSocket][WebSocketSession.ReadSocket] Failed to fetch the data to send"
            );
            return false;
        }

        var skipRelease = false;
        try
        {
            _sendSemaphore.Wait();

            var totalSentSize = 0;
            var remainingSize = l_ArraySegment.Count;

            while (remainingSize > 0)
            {
                var frameSize   = Math.Min(remainingSize, _maxFrameLength);
                var isLastFrame = frameSize == remainingSize;

                var sendTask = Socket.SendAsync(l_ArraySegment.Slice(totalSentSize, frameSize), type, isLastFrame, _cancellationTokenSource.Token);
                sendTask.ConfigureAwait(false);
                sendTask.Wait();

                totalSentSize += frameSize;
                remainingSize -= frameSize;
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

            _sendSemaphore.Release();
            skipRelease = true;

            Close(WebSocketCloseStatus.ProtocolError, "Error while writting", sendClosure: true, fromRemote: false);

            return false;
        }
        finally
        {
            if (!skipRelease)
                _sendSemaphore.Release();
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
        if (_closeSignaled)
            return;

        _closeSignaled = true;

        if (!_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();

        if (sendClosure && Socket != null && IsConnected)
        {
            _sendSemaphore.Wait();

            try
            {
                var closeTask = Socket.CloseOutputAsync(closeStatus ?? WebSocketCloseStatus.NormalClosure, reason ?? string.Empty, CancellationToken.None);
                closeTask.ConfigureAwait(false);
                closeTask.Wait();
            }
            catch (Exception)
            {
                /// Do nothing
            }
            finally
            {
                _sendSemaphore.Release();
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

        while (_receivedMessages.TryTake(out var pendingMessage))
            Allocator.Return(pendingMessage.data);
    }
}
