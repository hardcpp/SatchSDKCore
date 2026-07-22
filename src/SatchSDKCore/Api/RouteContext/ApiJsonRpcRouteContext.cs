using System.Diagnostics.CodeAnalysis;
using SSC.Api.Request;

namespace SSC.Api.RouteContext;

/// <summary>
/// JSON-RPC route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiJsonRpcRouteContext : ApiRouteContext
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public ApiJsonRpcRouteContext(ApiRequest request)
        : base(request)
    {
    }
}

public static class ApiJsonRpcRouteContextExtensions
{
    /// <summary>
    /// Cast ApiRouteContext to ApiJsonRpcRouteContext
    /// </summary>
    /// <param name="self">The ApiRouteContext instance</param>
    /// <returns>ApiJsonRpcRouteContext if the cast is successful, otherwise null</returns>
    public static ApiJsonRpcRouteContext? AsJsonRpcRouteContext(this ApiRouteContext self)
        => self as ApiJsonRpcRouteContext;
}
