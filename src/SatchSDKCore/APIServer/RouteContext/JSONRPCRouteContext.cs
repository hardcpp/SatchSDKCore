using System.Diagnostics.CodeAnalysis;

namespace SSC.APIServer.RouteContext;

/// <summary>
/// JSON-RPC route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class JSONRPCRouteContext : IRouteContext
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public JSONRPCRouteContext(Request.IRequest request)
        : base(request)
    {

    }
}
