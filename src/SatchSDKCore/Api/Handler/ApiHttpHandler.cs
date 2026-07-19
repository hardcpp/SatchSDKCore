using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Api.Handler;

/// <summary>
/// HTTP Server handler
/// </summary>
public class ApiHttpHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.ApiHttpBlueprint MainBlueprint = new("Main");

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly ThreadLocal<List<string>> _segmentBuffers = new(() => new List<string>(10));

    private readonly ThreadLocal<Dictionary<string, string>> _argumentsCollectors =
        new(() => new Dictionary<string, string>());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public bool TryHandle(HttpServerExRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpListenerRequest originalRequest = context.ListenerRequest;

        List<string> segments = GetSegmentsFromAbsolutePath(
            originalRequest.Url!.AbsolutePath);

        Dictionary<string, string>? arguments = _argumentsCollectors.Value!;
        arguments.Clear();

        // First match only the path. This lets us distinguish 404 from 405.
        if (!MainBlueprint.TryFindRoutes(
                CollectionsMarshal.AsSpan(segments),
                arguments,
                out ApiHttpRoute?[]? routes))
        {
            // The path does not exist. Let HttpServerExCore produce its
            // normal 404 response.
            return false;
        }

        // The path exists, but the client used an extension or unsupported
        // method such as PROPFIND.
        if (!TryGetHttpMethod(
                originalRequest.HttpMethod,
                out EApiHttpMethod httpMethod))
        {
            context.ServerResponse =
                CreateMethodNotAllowedResponse(routes);

            return true;
        }

        // Explicitly registered OPTIONS routes take priority.
        // Otherwise generate an automatic OPTIONS response.
        if (httpMethod == Route.EApiHttpMethod.Options &&
            routes[(int)Route.EApiHttpMethod.Options] == null)
        {
            context.ServerResponse =
                CreateAutomaticOptionsResponse(routes);

            return true;
        }

        ApiHttpRoute? route = routes[(int)httpMethod];

        // Explicit HEAD route takes priority. Otherwise execute GET and
        // suppress its response body.
        if (httpMethod == Route.EApiHttpMethod.Head &&
            route == null)
        {
            route = routes[(int)Route.EApiHttpMethod.Get];
        }

        if (route == null)
        {
            // The path exists, but not for this method.
            context.ServerResponse =
                CreateMethodNotAllowedResponse(routes);

            return true;
        }

        var httpRequest = new Request.ApiHttpRequest(context);

        var httpContext = new RouteContext.ApiHttpRouteContext(
            httpRequest,
            httpMethod);

        route.TryInvoke(
            httpContext,
            arguments,
            out string? error,
            out ApiResponse? response);

        if (response != null)
        {
            HttpServerExResponse? serverResponse =
                response.AsHttpResponse()?.HttpServerExResponse;

            if (serverResponse != null &&
                httpMethod == Route.EApiHttpMethod.Head)
            {
                serverResponse =
                    serverResponse.WithSuppressedBody();
            }

            context.ServerResponse = serverResponse;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            Logging.Log(
                ELogSeverity.Error,
                $"Failed to execute route '{route.HttpEndpoint}': {error}");

            HttpServerExResponse serverResponse = ApiHttpResponse.CodeResult(
                httpContext,
                HttpStatusCode.InternalServerError
            ).HttpServerExResponse;

            // HEAD responses must not contain a body, including error
            // responses.
            if (httpMethod == Route.EApiHttpMethod.Head)
            {
                serverResponse = serverResponse.WithSuppressedBody();
            }

            context.ServerResponse = serverResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Try to convert an HTTP method string to an API HTTP method.
    /// </summary>
    private static bool TryGetHttpMethod(
        string originalMethod,
        out Route.EApiHttpMethod method)
    {
        switch (originalMethod)
        {
            case "GET":
                method = Route.EApiHttpMethod.Get;
                return true;

            case "HEAD":
                method = Route.EApiHttpMethod.Head;
                return true;

            case "POST":
                method = Route.EApiHttpMethod.Post;
                return true;

            case "PUT":
                method = Route.EApiHttpMethod.Put;
                return true;

            case "PATCH":
                method = Route.EApiHttpMethod.Patch;
                return true;

            case "DELETE":
                method = Route.EApiHttpMethod.Delete;
                return true;

            case "OPTIONS":
                method = Route.EApiHttpMethod.Options;
                return true;

            default:
                method = default;
                return false;
        }
    }

    /// <summary>
    /// Create a 405 Method Not Allowed response.
    /// </summary>
    private static HttpServerExResponse CreateMethodNotAllowedResponse(
        Route.ApiHttpRoute?[] routes)
    {
        var headers = new Dictionary<string, string>(1) { ["Allow"] = BuildAllowHeader(routes) };

        return new HttpServerExResponse(
            HttpStatusCode.MethodNotAllowed,
            new StringContent(
                "405 Method Not Allowed",
                Encoding.UTF8,
                "text/plain"),
            Encoding.UTF8,
            headers);
    }

    /// <summary>
    /// Create an automatic OPTIONS response.
    /// </summary>
    private static HttpServerExResponse CreateAutomaticOptionsResponse(
        Route.ApiHttpRoute?[] routes)
    {
        var headers = new Dictionary<string, string>(1) { ["Allow"] = BuildAllowHeader(routes) };

        return new HttpServerExResponse(
            HttpStatusCode.NoContent,
            null,
            null,
            headers);
    }

    /// <summary>
    /// Build the Allow header for a matched route path.
    /// </summary>
    private static string BuildAllowHeader(
        Route.ApiHttpRoute?[] routes)
    {
        var result = new StringBuilder(48);

        bool HasRoute(Route.EApiHttpMethod method)
            => routes[(int)method] != null;

        void Append(string method)
        {
            if (result.Length > 0)
            {
                result.Append(", ");
            }

            result.Append(method);
        }

        if (HasRoute(Route.EApiHttpMethod.Get))
        {
            Append("GET");
        }

        // HEAD is automatically supported when GET exists.
        if (HasRoute(Route.EApiHttpMethod.Head) ||
            HasRoute(Route.EApiHttpMethod.Get))
        {
            Append("HEAD");
        }

        if (HasRoute(Route.EApiHttpMethod.Post))
        {
            Append("POST");
        }

        if (HasRoute(Route.EApiHttpMethod.Put))
        {
            Append("PUT");
        }

        if (HasRoute(Route.EApiHttpMethod.Patch))
        {
            Append("PATCH");
        }

        if (HasRoute(Route.EApiHttpMethod.Delete))
        {
            Append("DELETE");
        }

        // OPTIONS is automatically available for every matched path.
        Append("OPTIONS");

        return result.ToString();
    }

    /// <summary>
    /// Get splitted segment from an absolute Url
    /// </summary>
    /// <param name="absolutePath">Absolut Url</param>
    /// <returns>List of segments</returns>
    protected List<string> GetSegmentsFromAbsolutePath(string absolutePath)
    {
        List<string>? result = _segmentBuffers.Value!;
        result.Clear();

        for (int i = 0; i < absolutePath.Length; ++i)
        {
            if (absolutePath[i] == '/')
            {
                continue;
            }

            int nextSeparator = absolutePath.IndexOf('/', i);
            if (nextSeparator == -1)
            {
                result.Add(absolutePath[i..]);
                i = absolutePath.Length;
            }
            else
            {
                result.Add(absolutePath[i..nextSeparator]);
                i = nextSeparator;
            }
        }

        return result;
    }
}
