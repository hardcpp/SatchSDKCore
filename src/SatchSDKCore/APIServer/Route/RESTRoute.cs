using System;
using System.Diagnostics.CodeAnalysis;

namespace SSC.APIServer.Route;

/// <summary>
/// Rest method type
/// </summary>
public enum ERestMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

/// <summary>
/// Rest route attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class RESTRoute : IRoute
{
    public readonly ERestMethod RESTMethod;
    public readonly string      RESTEndpoint;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">REST Method</param>
    /// <param name="endpoint">REST path</param>
    /// <param name="asyncTimeoutStr">Timeout for async</param>
    public RESTRoute(ERestMethod method, string endpoint, string? asyncTimeoutStr = null)
        : base(asyncTimeoutStr)
    {
        if (string.IsNullOrEmpty(endpoint) || endpoint[0] != '/' || (endpoint.Length > 1 && endpoint[^1] == '/'))
            throw new Exception($"Malformated route path '{endpoint}'");

        RESTMethod   = method;
        RESTEndpoint = endpoint;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get exception response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="p_Exception">Exception if any</param>
    /// <returns></returns>
    protected override Response.IResponse GetResponseForException(RouteContext.IRouteContext routeContext, Exception p_Exception)
    {
        return Response.RESTResponse.Result(
            routeContext: routeContext,
            code:         System.Net.HttpStatusCode.InternalServerError,
            content:      "Internal error"
        );
    }
    /// <summary>
    /// Get response for bad request
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="error">Error message</param>
    /// <returns></returns>
    protected override Response.IResponse GetResponseForBadRequest(RouteContext.IRouteContext routeContext, string error)
    {
        return Response.RESTResponse.Result(
            routeContext: routeContext,
            code:         System.Net.HttpStatusCode.BadRequest,
            content:      $"Bad request: {error}"
        );
    }
    /// <summary>
    /// Get async timeout response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <returns></returns>
    protected override Response.IResponse GetResponseForAsyncTimeout(RouteContext.IRouteContext routeContext)
    {
        return Response.RESTResponse.Result(
            routeContext: routeContext,
            code:         System.Net.HttpStatusCode.RequestTimeout,
            content:      $"Request timeout"
        );
    }
}
