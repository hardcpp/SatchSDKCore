using System;

namespace SSC.APIServer.RouteHook;

/// <summary>
/// Route hook attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class IRouteHook : Attribute
{
    /// <summary>
    /// Try intercept the route
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="interceptionResult">Result to send if returned true</param>
    /// <returns>True if the route should be interrupted</returns>
    public abstract bool TryIntercept(RouteContext.IRouteContext routeContext, out Response.IResponse interceptionResult);
}