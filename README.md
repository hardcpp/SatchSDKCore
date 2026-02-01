# SatchSDKCore

[![Build Publish](https://github.com/hardcpp/SatchSDKCore/workflows/Build%20Publish/badge.svg)](https://github.com/hardcpp/SatchSDKCore/actions/workflows/build-publish.yml)
[![Security](https://github.com/hardcpp/SatchSDKCore/workflows/Security%20Scanning/badge.svg)](https://github.com/hardcpp/SatchSDKCore/actions/workflows/security.yml)
[![Code Quality](https://github.com/hardcpp/SatchSDKCore/workflows/Code%20Quality/badge.svg)](https://github.com/hardcpp/SatchSDKCore/actions/workflows/code-quality.yml)
[![Documentation](https://github.com/hardcpp/SatchSDKCore/workflows/Documentation/badge.svg)](https://github.com/hardcpp/SatchSDKCore/actions/workflows/documentation.yml)

UNDER CONSTRUCTION

SatchSDKCore is a high-performance .NET SDK for building REST APIs, WebSocket servers, and network applications. Built with .NET 10.0, it offers a robust foundation for creating scalable server applications with features optimized for speed and efficiency.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Components](#core-components)
  - [REST API Framework](#rest-api-framework)
  - [HTTP Server](#http-server)
  - [HTTP Client](#http-client)
  - [WebSocket Server](#websocket-server)
  - [JSON-RPC Client](#json-rpc-client)
  - [Configuration System](#configuration-system)
  - [Memory Pooling](#memory-pooling)
  - [Security Utilities](#security-utilities)
- [Performance](#performance)
- [Contributing](#contributing)
- [License](#license)

## Features

- **High-Performance REST API Framework**
  - Blueprint-based route organization
  - Attribute-based routing with path parameters
  - Flexible request/response handling
  - Built-in Swagger integration
  - Route hooks for middleware-like functionality

- **Advanced Networking**
  - High-performance HTTP server with worker pool
  - WebSocket server with session management
  - Advanced HTTP client with retry policies and rate limiting
  - JSON-RPC client for HTTP transport

- **Performance Optimizations**
  - AOT compilation support
  - Concurrent garbage collection
  - Native instruction set optimization
  - Thread-safe and single-threaded memory pooling
  - Efficient buffer management

- **Developer Tools**
  - JSON-based configuration system with auto-save
  - Type conversion utilities
  - Fast text encoding helpers
  - Comprehensive logging support

- **Security & Encoding**
  - TOTP (Time-based One Time Password) implementation
  - Base32 encoding/decoding
  - Built-in utilities for secure operations

## Installation

SatchSDKCore targets .NET 10.0. Install the package using:

```bash
dotnet add package SatchSDKCore
```

## Quick Start

Here's a minimal example to get started with a REST API server:

```csharp
using SSC.Api.Blueprint;
using SSC.Api.Handler;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;
using SSC.Net.HttpEx;
using System.Net;

// Define API routes
public class HelloRoutes
{
    [ApiHttpRoute(EApiHttpMethod.Get, "/hello")]
    public static ApiResponse SayHello(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.Result(context, HttpStatusCode.OK,
            "{\"message\":\"Hello, World!\"}");
    }
}

// Set up and start server
var blueprint = new ApiHttpBlueprint("API", "api/v1");
blueprint.AddRoutesOf<HelloRoutes>();

var handler = new ApiHttpHandler();
handler.MainBlueprint.AddBlueprint(blueprint);

var server = new HttpServerExCore("http://+:8080/", workerCount: 4);
server.AddRequestHandler(handler);
server.Start();

Console.WriteLine("Server running on http://localhost:8080/api/v1/hello");
server.Wait();
```

## Core Components

### REST API Framework

The REST API framework uses a blueprint-based architecture for organizing routes with attribute-based routing.

#### Basic Route Definition

```csharp
using SSC.Api.Blueprint;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;
using System.Net;

public class UserApiRoutes
{
    // GET /api/v1/users/<userId>
    [ApiHttpRoute(EApiHttpMethod.Get, "/users/<userId>")]
    public static ApiResponse GetUser(ApiHttpRouteContext context)
    {
        var userId = context.GetArgument("userId");

        // Your business logic here
        var userData = $"{{\"userId\":\"{userId}\",\"name\":\"John Doe\"}}";

        return ApiHttpResponse.Result(
            routeContext: context,
            code: HttpStatusCode.OK,
            content: userData
        );
    }

    // POST /api/v1/users
    [ApiHttpRoute(EApiHttpMethod.Post, "/users")]
    public static ApiResponse CreateUser(ApiHttpRouteContext context)
    {
        var requestBody = context.HttpRequest.BodyString;

        // Process request and create user

        return ApiHttpResponse.Result(
            routeContext: context,
            code: HttpStatusCode.Created,
            content: "{\"status\":\"created\",\"userId\":\"123\"}"
        );
    }

    // PUT /api/v1/users/<userId>
    [ApiHttpRoute(EApiHttpMethod.Put, "/users/<userId>")]
    public static ApiResponse UpdateUser(ApiHttpRouteContext context)
    {
        var userId = context.GetArgument("userId");
        var requestBody = context.HttpRequest.BodyString;

        // Update user logic

        return ApiHttpResponse.Result(context, HttpStatusCode.OK,
            "{\"status\":\"updated\"}");
    }

    // DELETE /api/v1/users/<userId>
    [ApiHttpRoute(EApiHttpMethod.Delete, "/users/<userId>")]
    public static ApiResponse DeleteUser(ApiHttpRouteContext context)
    {
        var userId = context.GetArgument("userId");

        // Delete user logic

        return ApiHttpResponse.Result(context, HttpStatusCode.NoContent);
    }
}
```

#### Blueprint Organization

```csharp
using SSC.Api.Blueprint;
using SSC.Api.Handler;

// Create main blueprint
var mainBlueprint = new ApiHttpBlueprint("MainAPI", "api");

// Create versioned sub-blueprints
var v1Blueprint = new ApiHttpBlueprint("V1", "v1");
v1Blueprint.AddRoutesOf<UserApiRoutes>();
v1Blueprint.AddRoutesOf<ProductApiRoutes>();

var v2Blueprint = new ApiHttpBlueprint("V2", "v2");
v2Blueprint.AddRoutesOf<UserApiV2Routes>();

// Organize blueprints hierarchically
mainBlueprint.AddBlueprint(v1Blueprint);
mainBlueprint.AddBlueprint(v2Blueprint);

// Create handler
var apiHandler = new ApiHttpHandler();
apiHandler.MainBlueprint.AddBlueprint(mainBlueprint);
```

#### Route Hooks (Middleware)

```csharp
using SSC.Api.RouteHook;
using SSC.Api.RouteContext;
using System.Net;

public class AuthenticationHook : ApiRouteHook
{
    public override bool TryIntercept(ApiRouteContext context)
    {
        if (context is ApiHttpRouteContext httpContext)
        {
            var authHeader = httpContext.HttpRequest.Headers["Authorization"];

            if (string.IsNullOrEmpty(authHeader))
            {
                httpContext.SetResult(ApiHttpResponse.Result(
                    httpContext,
                    HttpStatusCode.Unauthorized,
                    "{\"error\":\"Missing authorization header\"}"
                ));
                return true;
            }

            // Validate token...
        }

        return false;
    }
}

// Apply hook to blueprint
blueprint.Hooks.AddHook(new AuthenticationHook());
```

#### Swagger Integration

```csharp
using SSC.Api.Handler;

// Add Swagger handler to expose API documentation
var swaggerHandler = new ApiSwaggerHttpHandler(apiHandler);

server.AddRequestHandler(swaggerHandler); // Serves at /swagger
server.AddRequestHandler(apiHandler);     // Main API handler
```

### HTTP Server

High-performance HTTP server with multi-threaded request handling.

```csharp
using SSC.Net.HttpEx;

// Create server with 4 worker threads
var server = new HttpServerExCore(
    url: "http://+:8080/",
    workerCount: 4
);

// Add request handlers
server.AddRequestHandler(apiHandler);
server.AddRequestHandler(customHandler);

// Start server
server.Start();

Console.WriteLine("Server started successfully");

// Wait for server to stop (blocks until server.Stop() is called)
server.Wait();

// Or run without blocking
// await Task.Delay(-1); // Keep application running
```

### HTTP Client

Advanced HTTP client with automatic retry, rate limiting, and progress tracking.

#### Basic Usage

```csharp
using SSC.Net.HttpEx;
using System;

// Create client with base URL and timeout
var client = new HttpClientExCore(
    baseURL: "https://api.example.com",
    timeout: TimeSpan.FromSeconds(30)
);

// Configure retry policy
client.MaxRetry = 3;
client.RetryInterval = TimeSpan.FromSeconds(5);

// Simple GET request
var response = client.DoRequest("GET", "/users/123");

if (response.IsSuccessStatusCode)
{
    Console.WriteLine($"Response: {response.BodyString}");
}
else
{
    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
}
```

#### POST with Payload

```csharp
using SSC.Net.HttpEx;

var payload = HttpClientExPayload.FromJsonString(
    "{\"name\":\"John\",\"email\":\"john@example.com\"}"
);

var response = client.DoRequest(
    method: "POST",
    url: "/users",
    payload: payload
);
```

#### Async Requests with Progress

```csharp
using SSC.Net.HttpEx;
using System;
using System.Threading;
using System.Threading.Tasks;

var progress = new Progress<float>(p =>
    Console.WriteLine($"Download progress: {p:P0}")
);

var cts = new CancellationTokenSource();

var response = await client.DoRequestAsync(
    method: "GET",
    url: "/large-file.zip",
    cancellationToken: cts.Token,
    progressHandler: progress
);
```

#### Background Requests

```csharp
using SSC.Net.HttpEx;
using System.Threading;

client.DoRequestInBackground(
    method: "GET",
    url: "/notifications",
    cancellationToken: CancellationToken.None,
    callback: response =>
    {
        if (response?.IsSuccessStatusCode == true)
        {
            Console.WriteLine($"Notifications: {response.BodyString}");
        }
    }
);
```

#### Global Client

```csharp
using SSC.Net.HttpEx;

// Use the global shared client for simple requests
var response = HttpClientExCore.GlobalClient.DoRequest(
    "GET",
    "https://api.example.com/status"
);
```

### WebSocket Server

High-performance WebSocket server with session management and worker pool architecture.

#### Custom Session Implementation

```csharp
using SSC.Net.WebSocketEx;
using System.Net.WebSockets;
using System.Text;

public class ChatSession : WebSocketServerExSession<ChatSession, string>
{
    public string Username { get; set; } = "Guest";

    public ChatSession(
        WebSocketServerEx<ChatSession, string> server,
        WebSocket socket
    ) : base(server, socket, Guid.NewGuid().ToString())
    {
    }

    protected override void OnSessionOpen()
    {
        Console.WriteLine($"User {Username} connected (Session: {SessionID})");

        // Send welcome message
        SendAsync("{\"type\":\"welcome\",\"message\":\"Welcome to chat!\"}");
    }

    protected override void OnSocketMessage(ReadOnlySpan<byte> data, WebSocketMessageType type)
    {
        var text = Encoding.UTF8.GetString(data);
        Console.WriteLine($"[{Username}]: {text}");

        // Broadcast to all other sessions
        BroadcastMessage(text);
    }

    protected override void OnSessionRemove()
    {
        Console.WriteLine($"User {Username} disconnected");
    }

    private void BroadcastMessage(string message)
    {
        var broadcastData = $"{{\"from\":\"{Username}\",\"message\":\"{message}\"}}";
        var bytes = Encoding.UTF8.GetBytes(broadcastData);

        // Send to all sessions except this one
        var server = (WebSocketServerEx<ChatSession, string>)Server;
        server.FindSession(s => s != this)?.SendAsync(bytes);
    }

    private void SendAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        SendAsync(bytes, WebSocketMessageType.Text);
    }
}
```

#### WebSocket Server Setup

```csharp
using SSC.Net.WebSocketEx;
using SSC.Net.HttpEx;

// Create HTTP server
var httpServer = new HttpServerExCore("http://+:8080/", workerCount: 4);

// Create WebSocket server
var wsServer = new WebSocketServerEx<ChatSession, string>(
    httpServer: httpServer,
    absolutePath: "/chat",
    makeSession: (server, socket) => new ChatSession(server, socket),
    workerCount: 4,
    minConcurentSessions: 100,
    maxReceiveQueueSize: 50,
    maxFrameLength: 1024,
    maxMessageLength: 1024 * 1024 // 1MB
);

// Start servers
wsServer.Start();
httpServer.Start();

Console.WriteLine("WebSocket server running at ws://localhost:8080/chat");

httpServer.Wait();
```

#### Finding and Managing Sessions

```csharp
// Find specific session
var session = wsServer.FindSession(s => s.Username == "Alice");
if (session != null)
{
    session.SendAsync("Hello Alice!");
}
```

### JSON-RPC Client

JSON-RPC 2.0 client implementation with HTTP transport.

#### Basic Usage

```csharp
using SSC.Net.JsonRpc;
using SSC.Net.HttpEx;
using System.Text.Json.Nodes;

// Create HTTP client
var httpClient = new HttpClientExCore(
    baseURL: "https://api.example.com/rpc",
    timeout: TimeSpan.FromSeconds(10)
);

// Create JSON-RPC client
var rpcClient = new JsonRpcClientHttp(httpClient);

// Make RPC call
var result = rpcClient.Call(
    method: "getUser",
    @params: new JsonObject
    {
        ["userId"] = "123"
    }
);

if (result?.Error == null && result?.Result != null)
{
    Console.WriteLine($"User data: {result.Result}");
}
else
{
    Console.WriteLine($"RPC Error: {result?.Error}");
}
```

#### Async RPC Calls

```csharp
using SSC.Net.JsonRpc;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

var result = await rpcClient.CallAsync(
    method: "updateUser",
    cancellationToken: CancellationToken.None,
    @params: new JsonObject
    {
        ["userId"] = "123",
        ["name"] = "John Doe",
        ["email"] = "john@example.com"
    }
);
```

#### Background RPC Calls

```csharp
using SSC.Net.JsonRpc;
using System.Text.Json.Nodes;
using System.Threading;

rpcClient.CallInBackground(
    method: "sendNotification",
    cancellationToken: CancellationToken.None,
    callback: result =>
    {
        if (result?.Error == null)
        {
            Console.WriteLine("Notification sent successfully");
        }
    },
    @params: new JsonObject
    {
        ["userId"] = "123",
        ["message"] = "Hello!"
    }
);
```

### Configuration System

Type-safe JSON configuration with automatic persistence.

#### Define Configuration Class

```csharp
using SSC.Config;
using Newtonsoft.Json;

public class AppConfig : JsonConfig<AppConfig>
{
    // Configuration properties
    [JsonProperty("server_port")]
    public int ServerPort { get; set; } = 8080;

    [JsonProperty("api_key")]
    public string ApiKey { get; set; } = "";

    [JsonProperty("enable_logging")]
    public bool EnableLogging { get; set; } = true;

    [JsonProperty("max_connections")]
    public int MaxConnections { get; set; } = 100;

    [JsonProperty("database")]
    public DatabaseConfig Database { get; set; } = new();

    public class DatabaseConfig
    {
        [JsonProperty("host")]
        public string Host { get; set; } = "localhost";

        [JsonProperty("port")]
        public int Port { get; set; } = 5432;

        [JsonProperty("database")]
        public string DatabaseName { get; set; } = "myapp";
    }

    // Constructor specifies config file location
    public AppConfig() : base("./config")
    {
    }

    // Optional: Called when config is initialized
    protected override void OnInit(bool onFileCreate)
    {
        if (onFileCreate)
        {
            Console.WriteLine("Config file created with default values");
        }
        else
        {
            Console.WriteLine("Config file loaded successfully");
        }
    }
}
```

#### Using Configuration

```csharp
// Access singleton instance (auto-loads or creates config file)
var config = AppConfig.Instance;

// Read values
Console.WriteLine($"Server will run on port {config.ServerPort}");
Console.WriteLine($"Database: {config.Database.Host}:{config.Database.Port}");

// Modify values
config.ServerPort = 9000;
config.ApiKey = "new-secret-key";

// Save changes to disk
config.Save();

// Reset to defaults
config.Reset();
```

The configuration file (`AppConfig.json`) is automatically created in the specified directory:

```json
{
  "server_port": 8080,
  "api_key": "",
  "enable_logging": true,
  "max_connections": 100,
  "database": {
    "host": "localhost",
    "port": 5432,
    "database": "myapp"
  }
}
```

### Memory Pooling

SatchSDKCore includes sophisticated memory pooling to reduce GC pressure and improve performance.

#### Thread-Safe Pools

```csharp
using SSC.Pool;
using System.Collections.Generic;

// List pooling
var list = MTListPool<string>.Get();
try
{
    list.Add("item1");
    list.Add("item2");
    // Use the list
}
finally
{
    MTListPool<string>.Release(list); // Returns to pool
}
```

#### Single-Threaded Pools (Higher Performance)

```csharp
using SSC.Pool;

// For single-threaded scenarios (no locking overhead)
var list = STListPool<string>.Get();
try
{
    // Use the list
    list.Add("data");
}
finally
{
    STListPool<string>.Release(list);
}
```

#### Custom Object Pooling

```csharp
using SSC.Pool;

public class ExpensiveObject
{
    public byte[] Buffer { get; set; }

    public ExpensiveObject()
    {
        Buffer = new byte[1024 * 1024]; // 1MB buffer
    }

    public void Reset()
    {
        Array.Clear(Buffer, 0, Buffer.Length);
    }
}

// Create custom pool
var pool = new MTGenericPool<ExpensiveObject>(
    factory: () => new ExpensiveObject(),
    actionOnRelease: obj => obj.Reset(),
    defaultCapacity: 10
);

// Use pooled object
var obj = pool.Get();
try
{
    // Use the expensive object
}
finally
{
    pool.Release(obj);
}
```

#### Using PooledObject for Automatic Cleanup

```csharp
using SSC.Pool;

// Automatically returns to pool when disposed
using (var pooledList = MTListPool<string>.GetPooled())
{
    pooledList.Value.Add("item1");
    pooledList.Value.Add("item2");
    // List automatically returned to pool on dispose
}
```

### Security Utilities

#### TOTP (Time-based One Time Password)

```csharp
using SSC.Security;
using System.Text;

// Generate a secret key
byte[] secret = Encoding.UTF8.GetBytes("your-secret-key-here");

// Compute current TOTP code
string code = TOTP.ComputeCode(
    secret: secret,
    digits: 6,      // Number of digits in code
    period: 30,     // Time period in seconds
    windowOffset: 0 // Offset for time window
);

Console.WriteLine($"Current TOTP: {code}");

// Generate OTP Auth URL for QR code
string otpAuthUrl = TOTP.ForgeURL(
    label: "user@example.com",
    issuer: "MyApplication",
    secret: secret,
    digits: 6,
    period: 30
);

// Use this URL to generate a QR code that users can scan with authenticator apps
Console.WriteLine($"Scan this QR code: {otpAuthUrl}");
```

#### Verifying TOTP Codes

```csharp
using SSC.Security;
using System.Text;

byte[] secret = Encoding.UTF8.GetBytes("shared-secret");
string userProvidedCode = "123456";

// Check current window
string currentCode = TOTP.ComputeCode(secret);
bool isValid = currentCode == userProvidedCode;

// Check with time tolerance (previous and next window)
if (!isValid)
{
    // Check previous window
    string previousCode = TOTP.ComputeCode(secret, windowOffset: -1);
    isValid = previousCode == userProvidedCode;
}

if (!isValid)
{
    // Check next window
    string nextCode = TOTP.ComputeCode(secret, windowOffset: 1);
    isValid = nextCode == userProvidedCode;
}

Console.WriteLine($"Code is {(isValid ? "valid" : "invalid")}");
```

## Performance

SatchSDKCore is designed for high performance:

- **Server GC**: Optimized for server workloads with concurrent garbage collection
- **Memory Efficiency**: Thread-safe and single-threaded object pooling reduces allocations
- **Zero-Copy Operations**: Efficient use of Span<T> and Memory<T> where possible
- **Worker Pools**: Multi-threaded request handling for maximum throughput

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure:
- All tests pass (`dotnet test`)
- Code follows existing style conventions
- New features include appropriate tests
- Documentation is updated as needed

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Created and maintained by [HardCPP](https://github.com/hardcpp).

## Additional Resources

- [Testing Documentation](TESTING.md) - Comprehensive testing guide
- [GitHub Repository](https://github.com/hardcpp/SatchSDKCore)
- [Issue Tracker](https://github.com/hardcpp/SatchSDKCore/issues)

---

**Note**: This SDK requires .NET 10.0 or later. Ensure your development environment is properly configured before use.
