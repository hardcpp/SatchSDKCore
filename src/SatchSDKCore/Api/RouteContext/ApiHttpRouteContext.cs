using System.Diagnostics.CodeAnalysis;
using SSC.Api.Request;
using SSC.Api.Route;

namespace SSC.Api.RouteContext;

/// <summary>
/// HTTP route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiHttpRouteContext : ApiRouteContext
{
    public readonly EApiHttpMethod HttpMethod;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public ApiHttpRouteContext(ApiRequest request, EApiHttpMethod httpMethod)
        : base(request)
    {
        HttpMethod = httpMethod;
    }
}

public static class ApiHttpRouteContextExtensions
{
    /// <summary>
    /// Cast ApiRouteContext to ApiHttpRouteContext
    /// </summary>
    /// <param name="self">The ApiRouteContext instance</param>
    /// <returns>ApiHttpRouteContext if the cast is successful, otherwise null</returns>
    public static ApiHttpRouteContext? AsHttpRouteContext(this ApiRouteContext self)
        => self as ApiHttpRouteContext;
}
