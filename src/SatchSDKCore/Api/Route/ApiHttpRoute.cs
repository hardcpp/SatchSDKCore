using System;
using System.Diagnostics.CodeAnalysis;

namespace SSC.Api.Route;

/// <summary>
/// HTTP method type
/// </summary>
public enum EApiHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete,
    Head,
    Options
}

/// <summary>
/// HTTP route attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class ApiHttpRoute : ApiRoute
{
    public readonly EApiHttpMethod HttpMethod;
    public readonly string HttpEndpoint;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">HTTP Method</param>
    /// <param name="endpoint">HTTP path</param>
    /// <param name="asyncTimeoutStr">Timeout for async</param>
    public ApiHttpRoute(EApiHttpMethod method, string endpoint, string? asyncTimeoutStr = null)
        : base(asyncTimeoutStr)
    {
        if (string.IsNullOrEmpty(endpoint) || endpoint[0] != '/' || (endpoint.Length > 1 && endpoint[^1] == '/'))
            throw new Exception($"Malformated route path '{endpoint}'");

        HttpMethod = method;
        HttpEndpoint = endpoint;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get exception response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="p_Exception">Exception if any</param>
    /// <returns></returns>
    protected override Response.ApiResponse GetResponseForException(RouteContext.ApiRouteContext routeContext, Exception p_Exception)
    {
        return Response.ApiHttpResponse.Result(
            routeContext: routeContext,
            code: System.Net.HttpStatusCode.InternalServerError,
            content: "Internal error"
        );
    }
    /// <summary>
    /// Get response for bad request
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="error">Error message</param>
    /// <returns></returns>
    protected override Response.ApiResponse GetResponseForBadRequest(RouteContext.ApiRouteContext routeContext, string error)
    {
        return Response.ApiHttpResponse.Result(
            routeContext: routeContext,
            code: System.Net.HttpStatusCode.BadRequest,
            content: $"Bad request: {error}"
        );
    }
    /// <summary>
    /// Get async timeout response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <returns></returns>
    protected override Response.ApiResponse GetResponseForAsyncTimeout(RouteContext.ApiRouteContext routeContext)
    {
        return Response.ApiHttpResponse.Result(
            routeContext: routeContext,
            code: System.Net.HttpStatusCode.RequestTimeout,
            content: "Request timeout"
        );
    }
}
