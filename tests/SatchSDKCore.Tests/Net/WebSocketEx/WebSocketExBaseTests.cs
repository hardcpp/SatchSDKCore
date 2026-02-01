using System.Buffers;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using SSC.Net.WebSocketEx;

namespace SSC.Tests.Net.WebSocketEx;

/// <summary>
/// Tests for WebSocketExBase abstract class which provides common WebSocket functionality
/// with utilities for sending/receiving messages, managing connections, and handling closures.
/// </summary>
public class WebSocketExBaseTests
{
    /// <summary>
    /// Concrete test implementation of WebSocketExBase for testing purposes.
    /// Tracks lifecycle events and messages for verification.
    /// </summary>
    private class TestWebSocketEx : WebSocketExBase
    {
        public int OpenCallCount { get; private set; }
        public int MessageCallCount { get; private set; }
        public int CloseCallCount { get; private set; }
        public List<(byte[] data, WebSocketMessageType type)> ReceivedMessages { get; } = new();
        public WebSocketCloseStatus? LastCloseStatus { get; private set; }
        public string? LastCloseReason { get; private set; }
        public bool LastCloseFromRemote { get; private set; }

        public TestWebSocketEx(
            WebSocket? webSocket,
            ArrayPool<byte>? allocator = null,
            int maxFrameLength = 1024,
            int maxMessageLength = 5 * 1024 * 1024,
            int maxReceiveQueueSize = 50)
            : base(webSocket, allocator, maxFrameLength, maxMessageLength, maxReceiveQueueSize)
        {
        }

        protected override void OnSocketOpen()
        {
            OpenCallCount++;
        }

        protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type)
        {
            MessageCallCount++;
            ReceivedMessages.Add((data.ToArray(), type));
        }

        protected override void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote)
        {
            CloseCallCount++;
            LastCloseStatus = closeStatus;
            LastCloseReason = reason;
            LastCloseFromRemote = fromRemote;
        }

        // Expose protected methods for testing
        public new bool Send(string data, Encoding? encoding = null) => base.Send(data, encoding);
        public new bool Send(byte[] data, int offset = 0, int length = -1, WebSocketMessageType type = WebSocketMessageType.Binary)
            => base.Send(data, offset, length, type);
        public new bool Send(ReadOnlyMemory<byte> data, WebSocketMessageType type = WebSocketMessageType.Binary)
            => base.Send(data, type);
        public new void InternalStartRead() => base.InternalStartRead();
    }

    /// <summary>
    /// Mock WebSocket implementation for testing WebSocketExBase functionality.
    /// </summary>
    private class MockWebSocket : WebSocket
    {
        public override WebSocketState State { get; }
        public List<(ArraySegment<byte> buffer, WebSocketMessageType type, bool endOfMessage)> SentMessages { get; } = new();
        public WebSocketCloseStatus? CloseOutputStatus { get; private set; }
        public string? CloseOutputDescription { get; private set; }

        public MockWebSocket(WebSocketState state = WebSocketState.Open)
        {
            State = state;
        }

        public override void Abort() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            CloseOutputStatus = closeStatus;
            CloseOutputDescription = statusDescription;
            return Task.CompletedTask;
        }

        public override void Dispose() { }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // Return a close message by default
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SentMessages.Add((buffer, messageType, endOfMessage));
            return Task.CompletedTask;
        }

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
    }

    /// <summary>
    /// Verifies that constructor initializes WebSocketExBase with valid parameters
    /// and default values.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();

        // Act
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Assert
        Assert.NotNull(websocketEx);
        Assert.Equal(mockWebSocket, websocketEx.Socket);
        Assert.NotNull(websocketEx.Allocator);
        Assert.True(websocketEx.IsConnected);
    }

    /// <summary>
    /// Verifies that constructor accepts custom allocator.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomAllocator_UsesCustomAllocator()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var customAllocator = ArrayPool<byte>.Create(1024, 10);

        // Act
        var websocketEx = new TestWebSocketEx(mockWebSocket, customAllocator);

        // Assert
        Assert.Equal(customAllocator, websocketEx.Allocator);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentOutOfRangeException when
    /// maxMessageLength is less than maxFrameLength.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidMessageLength_ThrowsException()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TestWebSocketEx(mockWebSocket, maxFrameLength: 2048, maxMessageLength: 1024));
    }

    /// <summary>
    /// Verifies that sending a null or empty string throws ArgumentException.
    /// </summary>
    [Fact]
    public void Send_WithNullOrEmptyString_ThrowsException()
    {
        // Arrange
        var webSocket = new TestWebSocketEx(new MockWebSocket());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => webSocket.Send((string)null!));
        Assert.Throws<ArgumentException>(() => webSocket.Send(string.Empty));
    }

    /// <summary>
    /// Verifies that IsConnected returns false when WebSocket state is not Open.
    /// </summary>
    [Fact]
    public void IsConnected_WhenStateIsNotOpen_ReturnsFalse()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket(WebSocketState.Closed);
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act & Assert
        Assert.False(websocketEx.IsConnected);
    }

    /// <summary>
    /// Verifies that GetRemoteIPAddress returns null when RemoteEndPoint is not set.
    /// </summary>
    [Fact]
    public void GetRemoteIPAddress_WithoutRemoteEndPoint_ReturnsNull()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        var address = websocketEx.GetRemoteIPAddress();

        // Assert
        Assert.Null(address);
    }

    /// <summary>
    /// Verifies that Send with string data encodes and sends the message correctly.
    /// </summary>
    [Fact]
    public void Send_WithString_EncodesAndSendsMessage()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testMessage = "Hello WebSocket";

        // Act
        var result = websocketEx.Send(testMessage, Encoding.UTF8);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        var sentMessage = mockWebSocket.SentMessages[0];
        Assert.Equal(WebSocketMessageType.Text, sentMessage.type);
        Assert.True(sentMessage.endOfMessage);

        var decodedMessage = Encoding.UTF8.GetString(sentMessage.buffer.Array!, sentMessage.buffer.Offset, sentMessage.buffer.Count);
        Assert.Equal(testMessage, decodedMessage);
    }

    /// <summary>
    /// Verifies that Send with byte array sends the message correctly.
    /// </summary>
    [Fact]
    public void Send_WithByteArray_SendsMessage()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = websocketEx.Send(testData);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        var sentMessage = mockWebSocket.SentMessages[0];
        Assert.Equal(WebSocketMessageType.Binary, sentMessage.type);
        Assert.True(sentMessage.endOfMessage);
        Assert.Equal(testData.Length, sentMessage.buffer.Count);
    }

    /// <summary>
    /// Verifies that Send with byte array and offset/length sends the correct portion.
    /// </summary>
    [Fact]
    public void Send_WithByteArrayOffsetLength_SendsCorrectPortion()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var result = websocketEx.Send(testData, offset: 2, length: 4);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        var sentMessage = mockWebSocket.SentMessages[0];
        Assert.Equal(4, sentMessage.buffer.Count);
    }

    /// <summary>
    /// Verifies that Send with null byte array throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Send_WithNullByteArray_ThrowsException()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => websocketEx.Send((byte[])null!));
    }

    /// <summary>
    /// Verifies that Send splits large messages into multiple frames when message size exceeds maxFrameLength.
    /// </summary>
    [Fact]
    public void Send_WithLargeMessage_SplitsIntoFrames()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var maxFrameLength = 100;
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: maxFrameLength);
        var largeData = new byte[250];
        Array.Fill(largeData, (byte)42);

        // Act
        var result = websocketEx.Send(largeData);

        // Assert
        Assert.True(result);
        Assert.Equal(3, mockWebSocket.SentMessages.Count); // 100 + 100 + 50
        Assert.False(mockWebSocket.SentMessages[0].endOfMessage);
        Assert.False(mockWebSocket.SentMessages[1].endOfMessage);
        Assert.True(mockWebSocket.SentMessages[2].endOfMessage);
    }

    /// <summary>
    /// Verifies that Send returns false when WebSocket is not connected.
    /// </summary>
    [Fact]
    public void Send_WhenNotConnected_ReturnsFalse()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket(WebSocketState.Closed);
        var websocketEx = new TestWebSocketEx(mockWebSocket);
        var testData = new byte[] { 1, 2, 3 };

        // Act
        var result = websocketEx.Send(testData);

        // Assert
        Assert.False(result);
        Assert.Empty(mockWebSocket.SentMessages);
    }

    /// <summary>
    /// Verifies that Close method sets close signal and calls OnSocketClose.
    /// </summary>
    [Fact]
    public void Close_SetsCloseSignalAndCallsOnSocketClose()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        websocketEx.Close(WebSocketCloseStatus.NormalClosure, "Test close", sendClosure: false);

        // Assert
        Assert.Equal(1, websocketEx.CloseCallCount);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, websocketEx.LastCloseStatus);
        Assert.Equal("Test close", websocketEx.LastCloseReason);
        Assert.False(websocketEx.LastCloseFromRemote);
    }

    /// <summary>
    /// Verifies that Close sends closure message when sendClosure is true and socket is connected.
    /// </summary>
    [Fact]
    public void Close_WithSendClosure_SendsCloseMessage()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        websocketEx.Close(WebSocketCloseStatus.NormalClosure, "Closing", sendClosure: true);

        // Assert
        Assert.Equal(WebSocketCloseStatus.NormalClosure, mockWebSocket.CloseOutputStatus);
        Assert.Equal("Closing", mockWebSocket.CloseOutputDescription);
    }

    /// <summary>
    /// Verifies that calling Close multiple times only processes once.
    /// </summary>
    [Fact]
    public void Close_CalledMultipleTimes_OnlyProcessesOnce()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        websocketEx.Close(WebSocketCloseStatus.NormalClosure, "First", sendClosure: false);
        websocketEx.Close(WebSocketCloseStatus.EndpointUnavailable, "Second", sendClosure: false);

        // Assert
        Assert.Equal(1, websocketEx.CloseCallCount);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, websocketEx.LastCloseStatus);
        Assert.Equal("First", websocketEx.LastCloseReason);
    }

    /// <summary>
    /// Verifies that Send with ReadOnlyMemory works correctly.
    /// </summary>
    [Fact]
    public void Send_WithReadOnlyMemory_SendsMessage()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testData = new byte[] { 10, 20, 30, 40, 50 };
        var memory = new ReadOnlyMemory<byte>(testData);

        // Act
        var result = websocketEx.Send(memory, WebSocketMessageType.Binary);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        var sentMessage = mockWebSocket.SentMessages[0];
        Assert.Equal(WebSocketMessageType.Binary, sentMessage.type);
        Assert.Equal(testData.Length, sentMessage.buffer.Count);
    }

    /// <summary>
    /// Verifies that Send with byte array and invalid offset throws exception.
    /// </summary>
    [Fact]
    public void Send_WithInvalidOffset_ThrowsException()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => websocketEx.Send(testData, offset: 10, length: 2));
    }

    /// <summary>
    /// Verifies that Send with byte array and invalid length throws exception.
    /// </summary>
    [Fact]
    public void Send_WithInvalidLength_ThrowsException()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => websocketEx.Send(testData, offset: 0, length: 10));
    }

    /// <summary>
    /// Verifies that Send with text message type works correctly.
    /// </summary>
    [Fact]
    public void Send_WithTextMessageType_SendsAsText()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testData = Encoding.UTF8.GetBytes("Test message");
        var memory = new ReadOnlyMemory<byte>(testData);

        // Act
        var result = websocketEx.Send(memory, WebSocketMessageType.Text);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        Assert.Equal(WebSocketMessageType.Text, mockWebSocket.SentMessages[0].type);
    }

    /// <summary>
    /// Verifies that constructor with null WebSocket is allowed.
    /// </summary>
    [Fact]
    public void Constructor_WithNullWebSocket_AllowsNull()
    {
        // Arrange & Act
        var websocketEx = new TestWebSocketEx(null, maxFrameLength: 1024);

        // Assert
        Assert.Null(websocketEx.Socket);
        Assert.False(websocketEx.IsConnected);
    }

    /// <summary>
    /// Verifies that Send with very large message splits correctly across multiple frames.
    /// </summary>
    [Fact]
    public void Send_WithVeryLargeMessage_SplitsIntoManyFrames()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var maxFrameLength = 50;
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: maxFrameLength);
        var largeData = new byte[500];
        Array.Fill(largeData, (byte)99);

        // Act
        var result = websocketEx.Send(largeData);

        // Assert
        Assert.True(result);
        Assert.Equal(10, mockWebSocket.SentMessages.Count); // 500 / 50 = 10 frames

        // Verify all but last frame are not end of message
        for (int i = 0; i < mockWebSocket.SentMessages.Count - 1; i++)
        {
            Assert.False(mockWebSocket.SentMessages[i].endOfMessage);
        }

        // Verify last frame is end of message
        Assert.True(mockWebSocket.SentMessages[^1].endOfMessage);
    }

    /// <summary>
    /// Verifies that Send with byte array using default offset and length works.
    /// </summary>
    [Fact]
    public void Send_WithDefaultOffsetAndLength_SendsFullArray()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024);
        var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var result = websocketEx.Send(testData);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        Assert.Equal(testData.Length, mockWebSocket.SentMessages[0].buffer.Count);
    }

    /// <summary>
    /// Verifies that Close with null close status and reason works.
    /// </summary>
    [Fact]
    public void Close_WithNullStatusAndReason_UsesDefaults()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        websocketEx.Close(null, null, sendClosure: true);

        // Assert
        Assert.Equal(WebSocketCloseStatus.NormalClosure, mockWebSocket.CloseOutputStatus);
        Assert.Equal(string.Empty, mockWebSocket.CloseOutputDescription);
    }

    /// <summary>
    /// Verifies that Close from remote sets the correct flag.
    /// </summary>
    [Fact]
    public void Close_FromRemote_SetsRemoteFlag()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var websocketEx = new TestWebSocketEx(mockWebSocket);

        // Act
        websocketEx.Close(WebSocketCloseStatus.NormalClosure, "Remote close", sendClosure: false, fromRemote: true);

        // Assert
        Assert.Equal(1, websocketEx.CloseCallCount);
        Assert.True(websocketEx.LastCloseFromRemote);
    }

    /// <summary>
    /// Verifies that IsConnected is false when socket is null.
    /// </summary>
    [Fact]
    public void IsConnected_WhenSocketIsNull_ReturnsFalse()
    {
        // Arrange
        var websocketEx = new TestWebSocketEx(null);

        // Act & Assert
        Assert.False(websocketEx.IsConnected);
    }

    /// <summary>
    /// Verifies that constructor with maxFrameLength equal to maxMessageLength works.
    /// </summary>
    [Fact]
    public void Constructor_WithEqualFrameAndMessageLength_Succeeds()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();

        // Act
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: 1024, maxMessageLength: 1024);

        // Assert
        Assert.NotNull(websocketEx);
    }

    /// <summary>
    /// Verifies that Send with exact frame length sends in one frame.
    /// </summary>
    [Fact]
    public void Send_WithExactFrameLength_SendsInOneFrame()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();
        var maxFrameLength = 100;
        var websocketEx = new TestWebSocketEx(mockWebSocket, maxFrameLength: maxFrameLength);
        var exactData = new byte[maxFrameLength];
        Array.Fill(exactData, (byte)42);

        // Act
        var result = websocketEx.Send(exactData);

        // Assert
        Assert.True(result);
        Assert.Single(mockWebSocket.SentMessages);
        Assert.True(mockWebSocket.SentMessages[0].endOfMessage);
        Assert.Equal(maxFrameLength, mockWebSocket.SentMessages[0].buffer.Count);
    }
}
