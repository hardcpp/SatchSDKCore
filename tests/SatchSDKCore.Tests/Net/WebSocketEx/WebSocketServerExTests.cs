using System.Net;
using System.Net.WebSockets;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;
using SSC.Net.WebSocketEx;

namespace SSC.Tests.Net.WebSocketEx;

/// <summary>
/// Tests for WebSocketServerEx class which provides an advanced WebSocket server
/// with session management, worker threads, and HTTP integration.
/// </summary>
public class WebSocketServerExTests
{
    /// <summary>
    /// Test session implementation for testing WebSocketServerEx functionality.
    /// </summary>
    private class TestSession : WebSocketServerExSession<TestSession, string>
    {
        public int OpenCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int RemoveCallCount { get; private set; }

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

        protected override void OnSocketOpen() { }
        protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type) { }
        protected override void OnSocketClose(WebSocketCloseStatus? closeStatus, string? reason, bool fromRemote) { }
    }

    /// <summary>
    /// Mock HTTP server for testing WebSocketServerEx.
    /// </summary>
    private class MockHttpServer : IHttpServerEx
    {
        public List<IHttpServerExRequestHandler> Handlers { get; } = new();
        public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

        public void AddRequestHandler(IHttpServerExRequestHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            if (!Handlers.Contains(handler))
                Handlers.Add(handler);
        }

        public void RemoveRequestHandler(IHttpServerExRequestHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            Handlers.Remove(handler);
        }

        public void Start() { }
        public void Stop() { }
        public void Wait() { }
        public Task WaitAsync() => Task.CompletedTask;
        public ValueTask StopAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    /// <summary>
    /// Mock WebSocket implementation for testing.
    /// </summary>
    private class MockWebSocket : WebSocket
    {
        public override WebSocketState State { get; } = WebSocketState.Open;

        public override void Abort() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override void Dispose() { }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
    }


    /// <summary>
    /// Verifies that constructor creates WebSocketServerEx with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesServer()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act
        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Assert
        Assert.NotNull(wsServer);
        Assert.Equal(mockHttpServer, wsServer.HttpServer);
        Assert.Equal("/ws", wsServer.AbsolutePath);
        Assert.NotNull(wsServer.Hooks);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when httpServer is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullHttpServer_ThrowsException()
    {
        // Arrange
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketServerEx<TestSession, string>(null!, "/ws", sessionFactory));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when sessionFactory is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSessionFactory_ThrowsException()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebSocketServerEx<TestSession, string>(mockHttpServer, "/ws", null!));
    }

    /// <summary>
    /// Verifies that constructor throws UriFormatException when absolutePath doesn't start with '/'.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidAbsolutePath_ThrowsException()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.Throws<UriFormatException>(() =>
            new WebSocketServerEx<TestSession, string>(mockHttpServer, "ws", sessionFactory));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentOutOfRangeException when workerCount is less than 1.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidWorkerCount_ThrowsException()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WebSocketServerEx<TestSession, string>(mockHttpServer, "/ws", sessionFactory, workerCount: 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WebSocketServerEx<TestSession, string>(mockHttpServer, "/ws", sessionFactory, workerCount: -1));
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentOutOfRangeException when maxMessageLength is less than maxFrameLength.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidMessageLength_ThrowsException()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WebSocketServerEx<TestSession, string>(
                mockHttpServer,
                "/ws",
                sessionFactory,
                maxFrameLength: 2048,
                maxMessageLength: 1024));
    }

    /// <summary>
    /// Verifies that Stop method unregisters the server as a request handler.
    /// </summary>
    [Fact]
    public void Stop_UnregistersRequestHandler()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        wsServer.Start();

        // Act
        wsServer.Stop();

        // Assert
        Assert.DoesNotContain(wsServer, mockHttpServer.Handlers);
    }

    /// <summary>
    /// Verifies that calling Start multiple times doesn't cause issues.
    /// </summary>
    [Fact]
    public void Start_CalledMultipleTimes_NoError()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Act
        wsServer.Start();
        wsServer.Start(); // Second call should be safe

        // Assert
        Assert.Single(mockHttpServer.Handlers, h => h == wsServer);

        // Cleanup
        wsServer.Stop();
    }

    /// <summary>
    /// Verifies that calling Stop when not started doesn't cause issues.
    /// </summary>
    [Fact]
    public void Stop_WhenNotStarted_NoError()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Act & Assert - Should not throw
        wsServer.Stop();
    }

    /// <summary>
    /// Verifies that Dispose properly stops the server.
    /// </summary>
    [Fact]
    public void Dispose_StopsServer()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        wsServer.Start();

        // Act
        wsServer.Dispose();

        // Assert
        Assert.DoesNotContain(wsServer, mockHttpServer.Handlers);
    }

    /// <summary>
    /// Verifies that FindSession returns null when no sessions match the predicate.
    /// </summary>
    [Fact]
    public void FindSession_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Act
        var session = wsServer.FindSession(s => s.SessionID == "non-existent");

        // Assert
        Assert.Null(session);
    }

    /// <summary>
    /// Verifies that FindSession returns the default value when provided and no match found.
    /// </summary>
    [Fact]
    public void FindSession_WithNoMatchAndDefault_ReturnsDefault()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        var mockWebSocket = new MockWebSocket();
        var defaultSession = new TestSession(wsServer, mockWebSocket, "default");

        // Act
        var session = wsServer.FindSession(s => s.SessionID == "non-existent", defaultSession);

        // Assert
        Assert.Equal(defaultSession, session);
    }

    /// <summary>
    /// Verifies that server configuration properties are set correctly.
    /// </summary>
    [Fact]
    public void ServerConfiguration_PropertiesSetCorrectly()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        var maxReceiveQueueSize = 100;
        var maxFrameLength = 2048;
        var maxMessageLength = 10 * 1024 * 1024;

        // Act
        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/websocket",
            sessionFactory,
            workerCount: 8,
            maxReceiveQueueSize: maxReceiveQueueSize,
            maxFrameLength: maxFrameLength,
            maxMessageLength: maxMessageLength);

        // Assert
        Assert.Equal("/websocket", wsServer.AbsolutePath);
        Assert.Equal(maxReceiveQueueSize, wsServer.MaxReceiveQueueSize);
        Assert.Equal(maxFrameLength, wsServer.MaxFrameLength);
        Assert.Equal(maxMessageLength, wsServer.MaxMessageLength);
        Assert.NotNull(wsServer.Allocator);
    }

    /// <summary>
    /// Verifies that server can be created with different worker counts.
    /// </summary>
    [Fact]
    public void Constructor_WithDifferentWorkerCounts_CreatesServer()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act
        using var server1 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/ws1", sessionFactory, workerCount: 1);
        using var server2 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/ws2", sessionFactory, workerCount: 4);
        using var server3 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/ws3", sessionFactory, workerCount: 16);

        // Assert
        Assert.NotNull(server1);
        Assert.NotNull(server2);
        Assert.NotNull(server3);
    }

    /// <summary>
    /// Verifies that TryHandle throws ArgumentNullException when context is null.
    /// This is a real test that validates the argument validation in TryHandle.
    /// </summary>
    [Fact]
    public async Task TryHandle_WithNullContext_ThrowsException()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        wsServer.Start();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await wsServer.TryHandleAsync(null!));

        // Cleanup
        wsServer.Stop();
    }

    /// <summary>
    /// Verifies that constructor with minimum concurrent sessions creates appropriate allocator.
    /// </summary>
    [Fact]
    public void Constructor_WithMinConcurrentSessions_CreatesAllocator()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act
        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory,
            minConcurentSessions: 1000);

        // Assert
        Assert.NotNull(wsServer.Allocator);
    }

    /// <summary>
    /// Verifies that constructor with equal maxFrameLength and maxMessageLength works.
    /// </summary>
    [Fact]
    public void Constructor_WithEqualFrameAndMessageLength_Succeeds()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act
        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory,
            maxFrameLength: 1024,
            maxMessageLength: 1024);

        // Assert
        Assert.Equal(1024, wsServer.MaxFrameLength);
        Assert.Equal(1024, wsServer.MaxMessageLength);
    }

    /// <summary>
    /// Verifies that multiple servers can be created with different paths.
    /// </summary>
    [Fact]
    public void Constructor_WithDifferentPaths_CreatesMultipleServers()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        // Act
        using var server1 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/api/v1/ws", sessionFactory);
        using var server2 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/api/v2/ws", sessionFactory);
        using var server3 = new WebSocketServerEx<TestSession, string>(
            mockHttpServer, "/chat", sessionFactory);

        // Assert
        Assert.Equal("/api/v1/ws", server1.AbsolutePath);
        Assert.Equal("/api/v2/ws", server2.AbsolutePath);
        Assert.Equal("/chat", server3.AbsolutePath);
    }

    /// <summary>
    /// Verifies that Hooks property is accessible and not null.
    /// </summary>
    [Fact]
    public void Hooks_IsAccessibleAndNotNull()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Act & Assert
        Assert.NotNull(wsServer.Hooks);
        Assert.IsAssignableFrom<IHookable<HttpServerExRequestContext>>(wsServer.Hooks);
    }

    /// <summary>
    /// Verifies that server can start and stop correctly.
    /// </summary>
    [Fact]
    public void StartStop_WorksCorrectly()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        // Act & Assert
        wsServer.Start();
        Assert.Contains(wsServer, mockHttpServer.Handlers);

        wsServer.Stop();
        Assert.DoesNotContain(wsServer, mockHttpServer.Handlers);
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times is safe.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_IsSafe()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/ws",
            sessionFactory);

        wsServer.Start();

        // Act & Assert - Multiple dispose calls should be safe
        wsServer.Dispose();
        wsServer.Dispose(); // Should not throw
    }

    /// <summary>
    /// Verifies that server properties maintain their values after Start.
    /// </summary>
    [Fact]
    public void Properties_AfterStart_MaintainValues()
    {
        // Arrange
        var mockHttpServer = new MockHttpServer();
        var sessionFactory = new WebSocketServerEx<TestSession, string>.MakeSessionCallback(
            (server, webSocket) => new TestSession(server, webSocket, Guid.NewGuid().ToString()));

        using var wsServer = new WebSocketServerEx<TestSession, string>(
            mockHttpServer,
            "/websocket",
            sessionFactory,
            maxReceiveQueueSize: 75,
            maxFrameLength: 4096,
            maxMessageLength: 8 * 1024 * 1024);

        // Act
        wsServer.Start();

        // Assert
        Assert.Equal("/websocket", wsServer.AbsolutePath);
        Assert.Equal(75, wsServer.MaxReceiveQueueSize);
        Assert.Equal(4096, wsServer.MaxFrameLength);
        Assert.Equal(8 * 1024 * 1024, wsServer.MaxMessageLength);

        // Cleanup
        wsServer.Stop();
    }
}
