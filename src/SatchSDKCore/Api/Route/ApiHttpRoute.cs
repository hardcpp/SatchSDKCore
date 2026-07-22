using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using SSC.Api.Response;
using SSC.Api.RouteContext;

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
    public readonly string         HttpEndpoint;
    public readonly EApiHttpMethod HttpMethod;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">HTTP Method</param>
    /// <param name="endpoint">HTTP path</param>
    /// <param name="asyncTimeoutStr">Timeout for async</param>
    public ApiHttpRoute(
        EApiHttpMethod method,
        string         endpoint,
        string?        asyncTimeoutStr = null)
        : base(asyncTimeoutStr)
    {
        if (string.IsNullOrEmpty(endpoint) || endpoint[0] != '/' || (endpoint.Length > 1 && endpoint[^1] == '/'))
            throw new Exception($"Malformated route path '{endpoint}'");

        HttpMethod   = method;
        HttpEndpoint = endpoint;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    protected override ApiResponse GetResponseForException(ApiRouteContext routeContext, Exception exception)
    {
        return ApiHttpResponse.ContentResult(
            routeContext,
            HttpStatusCode.InternalServerError,
            "Internal error"
        );
    }

    /// <inheritdoc />
    protected override ApiResponse GetResponseForBadRequest(ApiRouteContext routeContext, string error)
    {
        return ApiHttpResponse.ContentResult(
            routeContext,
            HttpStatusCode.BadRequest,
            $"Bad request: {error}"
        );
    }

    /// <inheritdoc />
    protected override ApiResponse GetResponseForAsyncTimeout(ApiRouteContext routeContext)
    {
        return ApiHttpResponse.ContentResult(
            routeContext,
            HttpStatusCode.RequestTimeout,
            "Request timeout"
        );
    }
}
