using System.Diagnostics.CodeAnalysis;

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
    public ApiJsonRpcRouteContext(Request.ApiRequest request)
        : base(request)
    {

    }
}