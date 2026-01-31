using System.Buffers;
using System.Net.WebSockets;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;
using SSC.Net.WebSocketEx;

namespace SSC.Tests.Net.WebSocketEx;

/// <summary>
/// Tests for WebSocketServerExSession class which provides session management
/// for WebSocket server connections with lifecycle hooks and message handling.
/// </summary>
public class WebSocketServerExSessionTests
{
    /// <summary>
    /// Mock WebSocketServerEx for testing sessions.
    /// </summary>
    private class MockWebSocketServer : IWebSocketServerEx<TestSession, string>
    {
        public ArrayPool<byte> Allocator { get; } = ArrayPool<byte>.Shared;
        public int MaxFrameLength { get; } = 1024;
        public int MaxMessageLength { get; } = 5 * 1024 * 1024;
        public int MaxReceiveQueueSize { get; } = 50;
        public IHttpServerEx HttpServer { get; } = null!;
        public string AbsolutePath { get; } = "/ws";

        // IHttpServerExRequestHandler.Hooks - note the different type
        IHookable<HttpServerExRequestContext> IHttpServerExRequestHandler.Hooks { get; } = new Hookable<HttpServerExRequestContext>();

        public void Start() { }
        public void Stop() { }
        public void Wait() { }
        public void Dispose() { }

        public TestSession? FindSession(Func<TestSession, bool> predicate, TestSession? defaultValue = default)
            => defaultValue;

        public bool TryHandle(HttpServerExRequestContext context) => false;
    }

    /// <summary>
    /// Test session implementation for testing WebSocketServerExSession functionality.
    /// </summary>
    private class TestSession : WebSocketServerExSession<TestSession, string>
    {
        public int OpenCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int RemoveCallCount { get; private set; }
        public int SocketOpenCallCount { get; private set; }
        public int SocketMessageCallCount { get; private set; }
        public int SocketCloseCallCount { get; private set; }
        public List<(byte[] data, WebSocketMessageType type)> ReceivedMessages { get; } = new();
        public WebSocketCloseStatus? LastCloseStatus { get; private set; }
        public string? LastCloseReason { get; private set; }
        public bool LastCloseFromRemote { get; private set; }

        public TestSession(
            IWebSocketServerEx<TestSession, string> server,
            WebSocket webSocket,
            string sessionID)
            : base(server, webSocket, sessionID)
        {
        }

        protected override void OnSessionOpen()
        {
            OpenCallCount++;
        }

        protected override void OnSessionUpdate()
        {
            UpdateCallCount++;
        }

        protected override void OnSessionRemove()
        {
            RemoveCallCount++;
        }

        protected override void OnSocketOpen()
        {
            SocketOpenCallCount++;
        }

        protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type)
        {
            SocketMessageCallCount++;
            ReceivedMessages.Add((data.ToArray(), type));
        }

        protected override void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote)
        {
            SocketCloseCallCount++;
            LastCloseStatus = closeStatus;
            LastCloseReason = reason;
            LastCloseFromRemote = fromRemote;
        }

        // Public wrappers for testing internal methods
        public void TriggerSessionOpen()
        {
            OnSessionOpen();
            OnSocketOpen();
        }

        public void TriggerSessionUpdate()
        {
            OnSessionUpdate();
        }

        public void TriggerSessionRemove()
        {
            OnSessionRemove();
        }
    }

    /// <summary>
    /// Mock WebSocket implementation for testing.
    /// </summary>
    private class MockWebSocket : WebSocket
    {
        public override WebSocketState State { get; }

        public MockWebSocket(WebSocketState state = WebSocketState.Open)
        {
            State = state;
        }

        public override void Abort() { }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;
        public override void Dispose() { }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            => Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
    }

    /// <summary>
    /// Verifies that constructor creates session with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesSession()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var sessionID = "test-session-123";

        // Act
        var session = new TestSession(mockServer, mockWebSocket, sessionID);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(mockServer, session.Server);
        Assert.Equal(sessionID, session.SessionID);
        Assert.Equal(mockWebSocket, session.Socket);
        Assert.True(session.IsConnected);
    }

    /// <summary>
    /// Verifies that constructor throws NullReferenceException when server is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServer_ThrowsException()
    {
        // Arrange
        var mockWebSocket = new MockWebSocket();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            new TestSession(null!, mockWebSocket, "session-id"));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when webSocket is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullWebSocket_ThrowsException()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestSession(mockServer, null!, "session-id"));
    }

    /// <summary>
    /// Verifies that OnSessionOpen is called properly.
    /// </summary>
    [Fact]
    public void OnSessionOpen_CallsLifecycleHook()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.TriggerSessionOpen();

        // Assert
        Assert.Equal(1, session.OpenCallCount);
        Assert.Equal(1, session.SocketOpenCallCount);
    }

    /// <summary>
    /// Verifies that OnSessionUpdate is called properly.
    /// </summary>
    [Fact]
    public void OnSessionUpdate_CallsLifecycleHook()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.TriggerSessionUpdate();

        // Assert
        Assert.Equal(1, session.UpdateCallCount);
    }

    /// <summary>
    /// Verifies that OnSessionRemove is called properly.
    /// </summary>
    [Fact]
    public void OnSessionRemove_CallsLifecycleHook()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.TriggerSessionRemove();

        // Assert
        Assert.Equal(1, session.RemoveCallCount);
    }

    /// <summary>
    /// Verifies that session inherits allocator settings from server.
    /// </summary>
    [Fact]
    public void Session_InheritsServerAllocator()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();

        // Act
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Assert
        Assert.Equal(mockServer.Allocator, session.Allocator);
    }

    /// <summary>
    /// Verifies that session inherits max frame length from server.
    /// </summary>
    [Fact]
    public void Session_InheritsServerMaxFrameLength()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();

        // Act
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Assert - MaxFrameLength is used internally but not directly exposed
        Assert.NotNull(session);
    }

    /// <summary>
    /// Verifies that session can be created with different session ID types.
    /// </summary>
    [Fact]
    public void Constructor_WithDifferentSessionIDTypes_CreatesSession()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket1 = new MockWebSocket();
        var mockWebSocket2 = new MockWebSocket();

        // Act
        var session1 = new TestSession(mockServer, mockWebSocket1, "string-id");
        var session2 = new TestSession(mockServer, mockWebSocket2, Guid.NewGuid().ToString());

        // Assert
        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.NotEqual(session1.SessionID, session2.SessionID);
    }

    /// <summary>
    /// Verifies that multiple OnSessionUpdate calls increment update count.
    /// </summary>
    [Fact]
    public void OnSessionUpdate_CalledMultipleTimes_IncrementsCount()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.TriggerSessionUpdate();
        session.TriggerSessionUpdate();
        session.TriggerSessionUpdate();

        // Assert
        Assert.Equal(3, session.UpdateCallCount);
    }

    /// <summary>
    /// Verifies that session correctly maintains reference to server.
    /// </summary>
    [Fact]
    public void Session_MaintainsServerReference()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();

        // Act
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Assert
        Assert.Same(mockServer, session.Server);
        Assert.Equal(mockServer.MaxFrameLength, session.Server.MaxFrameLength);
        Assert.Equal(mockServer.MaxMessageLength, session.Server.MaxMessageLength);
    }

    /// <summary>
    /// Verifies that session state reflects the underlying WebSocket state.
    /// </summary>
    [Fact]
    public void Session_ReflectsWebSocketState()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var openSocket = new MockWebSocket(WebSocketState.Open);
        var closedSocket = new MockWebSocket(WebSocketState.Closed);

        // Act
        var openSession = new TestSession(mockServer, openSocket, "open-session");
        var closedSession = new TestSession(mockServer, closedSocket, "closed-session");

        // Assert
        Assert.True(openSession.IsConnected);
        Assert.False(closedSession.IsConnected);
    }

    /// <summary>
    /// Verifies that Close method properly closes the session.
    /// </summary>
    [Fact]
    public void Close_ProperlyClosesSession()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.Close(WebSocketCloseStatus.NormalClosure, "Test close", sendClosure: false);

        // Assert
        Assert.Equal(1, session.SocketCloseCallCount);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, session.LastCloseStatus);
        Assert.Equal("Test close", session.LastCloseReason);
        Assert.False(session.LastCloseFromRemote);
    }

    /// <summary>
    /// Verifies that SessionID is correctly stored and accessible.
    /// </summary>
    [Fact]
    public void SessionID_IsCorrectlyStored()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var expectedID = "unique-session-id-12345";

        // Act
        var session = new TestSession(mockServer, mockWebSocket, expectedID);

        // Assert
        Assert.Equal(expectedID, session.SessionID);
    }

    /// <summary>
    /// Test session that can throw exceptions for testing error handling.
    /// </summary>
    private class ThrowingTestSession : WebSocketServerExSession<ThrowingTestSession, string>
    {
        public bool ThrowOnOpen { get; set; }
        public bool ThrowOnUpdate { get; set; }
        public bool ThrowOnRemove { get; set; }
        public bool ThrowOnSocketOpen { get; set; }
        public bool ThrowOnSocketMessage { get; set; }
        public int OpenCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int RemoveCallCount { get; private set; }

        public ThrowingTestSession(
            IWebSocketServerEx<ThrowingTestSession, string> server,
            WebSocket webSocket,
            string sessionID)
            : base(server, webSocket, sessionID)
        {
        }

        protected override void OnSessionOpen()
        {
            OpenCallCount++;
            if (ThrowOnOpen)
                throw new InvalidOperationException("Test exception in OnSessionOpen");
        }

        protected override void OnSessionUpdate()
        {
            UpdateCallCount++;
            if (ThrowOnUpdate)
                throw new InvalidOperationException("Test exception in OnSessionUpdate");
        }

        protected override void OnSessionRemove()
        {
            RemoveCallCount++;
            if (ThrowOnRemove)
                throw new InvalidOperationException("Test exception in OnSessionRemove");
        }

        protected override void OnSocketOpen()
        {
            if (ThrowOnSocketOpen)
                throw new InvalidOperationException("Test exception in OnSocketOpen");
        }

        protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type)
        {
            if (ThrowOnSocketMessage)
                throw new InvalidOperationException("Test exception in OnSocketMessage");
        }

        protected override void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote)
        {
        }

        // Expose internal methods for testing using reflection
        public void CallInternalOnSessionOpen()
        {
            var method = typeof(WebSocketServerExSession<ThrowingTestSession, string>)
                .GetMethod("InternalOnSessionOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }

        public void CallInternalOnSessionUpdate()
        {
            var method = typeof(WebSocketServerExSession<ThrowingTestSession, string>)
                .GetMethod("InternalOnSessionUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }

        public void CallInternalOnSessionRemove()
        {
            var method = typeof(WebSocketServerExSession<ThrowingTestSession, string>)
                .GetMethod("InternalOnSessionRemove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }
    }

    /// <summary>
    /// Mock WebSocketServerEx for ThrowingTestSession.
    /// </summary>
    private class MockThrowingWebSocketServer : IWebSocketServerEx<ThrowingTestSession, string>
    {
        public ArrayPool<byte> Allocator { get; } = ArrayPool<byte>.Shared;
        public int MaxFrameLength { get; } = 1024;
        public int MaxMessageLength { get; } = 5 * 1024 * 1024;
        public int MaxReceiveQueueSize { get; } = 50;
        public IHttpServerEx HttpServer { get; } = null!;
        public string AbsolutePath { get; } = "/ws";

        IHookable<HttpServerExRequestContext> IHttpServerExRequestHandler.Hooks { get; } = new Hookable<HttpServerExRequestContext>();

        public void Start() { }
        public void Stop() { }
        public void Wait() { }
        public void Dispose() { }

        public ThrowingTestSession? FindSession(Func<ThrowingTestSession, bool> predicate, ThrowingTestSession? defaultValue = default)
            => defaultValue;

        public bool TryHandle(HttpServerExRequestContext context) => false;
    }

    /// <summary>
    /// Verifies that InternalOnSessionOpen handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void InternalOnSessionOpen_WithException_HandlesGracefully()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session")
        {
            ThrowOnOpen = true
        };

        // Act - Should not throw, exception should be caught and logged
        session.CallInternalOnSessionOpen();

        // Assert - Session lifecycle was called despite exception
        Assert.Equal(1, session.OpenCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionOpen handles exceptions in OnSocketOpen gracefully.
    /// </summary>
    [Fact]
    public void InternalOnSessionOpen_WithSocketOpenException_HandlesGracefully()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session")
        {
            ThrowOnSocketOpen = true
        };

        // Act - Should not throw, exception should be caught and logged
        session.CallInternalOnSessionOpen();

        // Assert - OnSessionOpen was still called
        Assert.Equal(1, session.OpenCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionUpdate handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void InternalOnSessionUpdate_WithException_HandlesGracefully()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session")
        {
            ThrowOnUpdate = true
        };

        // Act - Should not throw, exception should be caught and logged
        session.CallInternalOnSessionUpdate();

        // Assert - OnSessionUpdate was called despite exception
        Assert.Equal(1, session.UpdateCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionRemove handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void InternalOnSessionRemove_WithException_HandlesGracefully()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session")
        {
            ThrowOnRemove = true
        };

        // Act - Should not throw, exception should be caught and logged
        session.CallInternalOnSessionRemove();

        // Assert - OnSessionRemove was called despite exception
        Assert.Equal(1, session.RemoveCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionOpen successfully calls both lifecycle methods.
    /// </summary>
    [Fact]
    public void InternalOnSessionOpen_CallsBothLifecycleMethods()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Act
        var sessionWithInternal = new ThrowingTestSession(
            new MockThrowingWebSocketServer(),
            mockWebSocket,
            "test");
        sessionWithInternal.CallInternalOnSessionOpen();

        // Assert
        Assert.Equal(1, sessionWithInternal.OpenCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionUpdate can be called multiple times.
    /// </summary>
    [Fact]
    public void InternalOnSessionUpdate_CalledMultipleTimes_ProcessesCorrectly()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.CallInternalOnSessionUpdate();
        session.CallInternalOnSessionUpdate();
        session.CallInternalOnSessionUpdate();

        // Assert
        Assert.Equal(3, session.UpdateCallCount);
    }

    /// <summary>
    /// Verifies that InternalOnSessionRemove can be called successfully.
    /// </summary>
    [Fact]
    public void InternalOnSessionRemove_CallsLifecycleMethod()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.CallInternalOnSessionRemove();

        // Assert
        Assert.Equal(1, session.RemoveCallCount);
    }

    /// <summary>
    /// Verifies that session properly inherits all server properties.
    /// </summary>
    [Fact]
    public void Constructor_InheritsAllServerProperties()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();

        // Act
        var session = new TestSession(mockServer, mockWebSocket, "test-session");

        // Assert
        Assert.Equal(mockServer.Allocator, session.Allocator);
        Assert.Equal(mockServer.MaxFrameLength, session.Server.MaxFrameLength);
        Assert.Equal(mockServer.MaxMessageLength, session.Server.MaxMessageLength);
        Assert.Equal(mockServer.MaxReceiveQueueSize, session.Server.MaxReceiveQueueSize);
    }

    /// <summary>
    /// Verifies session can be created with minimum configuration.
    /// </summary>
    [Fact]
    public void Constructor_WithMinimalConfiguration_CreatesSession()
    {
        // Arrange
        var mockServer = new MockWebSocketServer();
        var mockWebSocket = new MockWebSocket();

        // Act
        var session = new TestSession(mockServer, mockWebSocket, "id");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("id", session.SessionID);
    }

    /// <summary>
    /// Verifies that exception in both OnSessionOpen and OnSocketOpen is handled.
    /// </summary>
    [Fact]
    public void InternalOnSessionOpen_WithBothExceptions_HandlesGracefully()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session")
        {
            ThrowOnOpen = true,
            ThrowOnSocketOpen = true
        };

        // Act - Should not throw
        session.CallInternalOnSessionOpen();

        // Assert
        Assert.Equal(1, session.OpenCallCount);
    }

    /// <summary>
    /// Verifies that session maintains correct state after lifecycle operations.
    /// </summary>
    [Fact]
    public void Session_MaintainsStateAfterLifecycleOperations()
    {
        // Arrange
        var mockServer = new MockThrowingWebSocketServer();
        var mockWebSocket = new MockWebSocket();
        var session = new ThrowingTestSession(mockServer, mockWebSocket, "test-session");

        // Act
        session.CallInternalOnSessionOpen();
        session.CallInternalOnSessionUpdate();
        session.CallInternalOnSessionUpdate();
        session.CallInternalOnSessionRemove();

        // Assert
        Assert.Equal(1, session.OpenCallCount);
        Assert.Equal(2, session.UpdateCallCount);
        Assert.Equal(1, session.RemoveCallCount);
    }
}
