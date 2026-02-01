using System;

namespace SSC.Api.RouteHook;

/// <summary>
/// Route hook attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class ApiRouteHook : Attribute
{
    /// <summary>
    /// Try intercept the route
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="interceptionResult">Result to send if returned true</param>
    /// <returns>True if the route should be interrupted</returns>
    public abstract bool TryIntercept(RouteContext.ApiRouteContext routeContext, out Response.ApiResponse interceptionResult);
}
