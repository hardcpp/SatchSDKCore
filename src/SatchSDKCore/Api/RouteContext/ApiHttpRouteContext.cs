using System.Diagnostics.CodeAnalysis;

namespace SSC.Api.RouteContext;

/// <summary>
/// HTTP route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiHttpRouteContext : ApiRouteContext
{
    public readonly Route.EApiHttpMethod HttpMethod;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public ApiHttpRouteContext(Request.ApiRequest request, Route.EApiHttpMethod httpMethod)
        : base(request)
    {
        HttpMethod = httpMethod;
    }
}
