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
