using System.Diagnostics.CodeAnalysis;

namespace SSC.APIServer.RouteContext;

/// <summary>
/// REST route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class RESTRouteContext : IRouteContext
{
    public readonly Route.ERestMethod RestMethod;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public RESTRouteContext(Request.IRequest request, Route.ERestMethod restMethod)
        : base(request)
    {
        RestMethod = restMethod;
    }
}
