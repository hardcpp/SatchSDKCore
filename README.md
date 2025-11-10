# SatchSDKCore

SatchSDKCore is a high-performance .NET SDK for building REST APIs and network applications. Built with .NET 9.0, it offers a robust foundation for creating scalable server applications with features optimized for speed and efficiency.

## Features

- **High-Performance REST API Framework**
  - Blueprint-based route organization
  - Support for path parameters and dynamic routing
  - Flexible request/response handling
  - Swagger integration

- **Advanced Networking**
  - HTTP server implementation
  - WebSocket server support
  - Efficient request handling

- **Performance Optimizations**
  - AOT compilation support
  - Concurrent garbage collection
  - Native instruction set optimization
  - Memory pooling system

- **Additional Utilities**
  - JSON configuration support
  - AES encryption
  - TOTP implementation
  - Base32 encoding
  - Fast text encoding
  - Type conversion utilities

## Installation

SatchSDKCore targets .NET 9.0. To install the package, use the following command in your project directory:

```bash
dotnet add package SatchSDKCore
```

## Basic Usage

### Creating a REST API

```csharp
using SSC.APIServer.Blueprint;
using SSC.APIServer.Route;
using SSC.APIServer.Response;

// Create a blueprint for your API
var blueprint = new RESTBlueprint("MyAPI", "api/v1");

// Add a route
blueprint.AddRoute(new RESTRoute(
    ERestMethod.Get,  // Note: Enum values start with uppercase
    "/users/<userId>", // Note: Path must start with /
    async (context) => {
        var userId = context.GetArgument("userId");
        // Handle the request
        return RESTResponse.Result(
            routeContext: context,
            code: System.Net.HttpStatusCode.OK,
            content: "{\"status\":\"success\"}"
        );
    }
));
```

### Using the HTTP Server

```csharp
using SSC.Network.HTTP;

// Create and configure the HTTP server
var server = new HTTPServer("http://+:8080/"); // Listen on all interfaces
server.AddRequestHandler(new RESTHTTPServerHandler(blueprint));

// Start the server
server.Start();
server.Wait(); // Optional: Wait for the server to stop
```

## Advanced Features

### Memory Pooling

SatchSDKCore includes a sophisticated memory pooling system to reduce GC pressure:

```csharp
using SSC.Pool;

// Use thread-safe list pool
var list = MTListPool<string>.Get();
try
{
    // Use the pooled list
    list.Add("item");
}
finally
{
    // Return list to pool
    MTListPool<string>.Release(list);
}
```

### Security

Built-in security features:

```csharp
using SSC.Security;
using System.Text;

// AES encryption with CBC mode and inline IV
byte[] key = Encoding.UTF8.GetBytes("your-key-here");  // Key must be >= 6 bytes
byte[] data = Encoding.UTF8.GetBytes("sensitive data");

byte[] encrypted = AES.EncryptCBCInlineIV(key, data);
byte[] decrypted = AES.DecryptCBCInlineIV(key, encrypted);

// TOTP implementation
var totp = new TOTP("secretKey");
var code = totp.GenerateCode();
```

## Performance

SatchSDKCore is designed for high performance:
- Native AOT compilation support
- Server-optimized garbage collection
- Speed-focused optimization preferences
- Native instruction set utilization
- Efficient memory pooling

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Created and maintained by [HardCPP](https://github.com/hardcpp).
