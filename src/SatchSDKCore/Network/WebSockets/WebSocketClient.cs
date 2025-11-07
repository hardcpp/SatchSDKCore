using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Network.WebSockets;

/// <summary>
/// WebSocket client
/// </summary>
public class WebSocketClient : WebSocketCommon
{
    private ClientWebSocket? m_Client;
    private DateTime?        m_StartTime;
    private int              m_CurrentReconnectDelay;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public Uri? Endpoint                { get; private set; }
    public bool AutoReconnect           { get;         set; } = true;
    public int  ReconnectDelayIncrement { get;         set; } = 5;
    public int  ReconnectMaxDelay       { get;         set; } = 60;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public event Action<WebSocketClient>?
        OnConnected;
    public event Action<WebSocketClient, ReadOnlySpan<byte>, WebSocketMessageType>?
        OnMessage;
    public event Action<WebSocketClient, WebSocketCloseStatus?, string?, bool>?
        OnDisconnected;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="allocator">Allocator, using shared if null</param>
    /// <param name="maxFrameLength">Max length of a single frame</param>
    /// <param name="maxMessageLength">Max length of a single message</param>
    public WebSocketClient(
        ArrayPool<byte>? allocator        = null,
        int              maxFrameLength   = 1024,
        int              maxMessageLength = 5 * 1024 * 1024
        ) : base(null, allocator, maxFrameLength, maxMessageLength)
    {
        m_CurrentReconnectDelay = 0;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Connect async
    /// </summary>
    /// <param name="endpoint">Target endpoint</param>
    /// <param name="headers">Optional headers</param>
    public Task ConnectAsync(Uri endpoint, Dictionary<string, string>? headers)
    {
        if (IsConnected)
        {
            /// todo
        }

        ArgumentNullException.ThrowIfNull(endpoint);

        if (!m_CancellationTokenSource.IsCancellationRequested)
            m_CancellationTokenSource.Cancel();

        m_Client                  = new ClientWebSocket();
        m_StartTime               = DateTime.UtcNow;
        m_CancellationTokenSource = new CancellationTokenSource();

        Socket = null;

        var l_Task = Task.Run(async () =>
        {
            await Task.Yield();

            do
            {
                try
                {
                    if (headers != null)
                    {
                        foreach (var l_KVP in headers)
                            m_Client.Options.SetRequestHeader(l_KVP.Key, l_KVP.Value);
                    }

                    var l_ConnectCancellationTokenSource = new CancellationTokenSource();
                    await m_Client.ConnectAsync(endpoint, l_ConnectCancellationTokenSource.Token).ConfigureAwait(false);
                    l_ConnectCancellationTokenSource.CancelAfter(10);

                    if (m_Client != null && m_Client.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        /// Reset the reconnect delay
                        m_CurrentReconnectDelay = 0;

                        Socket              = m_Client;
                        Endpoint            = endpoint;
                        NegociatingHeaders  = new();

                        if (headers != null)
                        {
                            foreach (var item in headers)
                                NegociatingHeaders.Add(item.Key.ToString(), item.Value.ToString());
                        }

                        OnSocketOpen();
                    }
                    else
                        Close(WebSocketCloseStatus.EndpointUnavailable, string.Empty, sendClosure: false, fromRemote: false);
                }
                catch (TaskCanceledException)
                {
                    Logging.Log(ELogSeverity.Warning, "[Network.WebSocket][WebSocketClient.Connect] WebSocket client task was cancelled");
                }
                catch (Exception l_Exception)
                {
                    Logging.Log(ELogSeverity.Error, $"[Network.WebSocket][WebSocketClient.Connect] An exception occurred in WebSocket while connecting to {Endpoint}");
                    Logging.Log(ELogSeverity.Error, l_Exception);

                    OnError?.Invoke();

                    if (!m_Disconnecting)
                        TryHandleReconnect();
                }
            } while (true);
        });
        l_Task.ConfigureAwait(false);

        return l_Task;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On session open
    /// </summary>
    protected override void OnSocketOpen()
    {
        /// Start reading
        InternalStartRead();

        OnConnected?.Invoke(this);
    }
    /// <summary>
    /// On socket message
    /// </summary>
    /// <param name="data">Message data</param>
    /// <param name="type">Message type</param>
    protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type)
    {
        OnMessage?.Invoke(this, data, type);
    }
    /// <summary>
    /// On socket close
    /// </summary>
    /// <param name="closeStatus">Close status</param>
    /// <param name="reason">Close reason</param>
    /// <param name="fromRemote">Is the closure originated from remote?</param>
    protected override void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote)
    {
        Socket   = null;
        Endpoint = null;

        if (!fromRemote && AutoReconnect)
        {

        }

        OnDisconnected?.Invoke(this, closeStatus, reason, fromRemote);
    }
}
