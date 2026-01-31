using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using SSC.Api.Response;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Api.Handler;

/// <summary>
/// HTTP Server handler
/// </summary>
public class ApiHttpHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.ApiHttpBlueprint MainBlueprint = new Blueprint.ApiHttpBlueprint("Main");

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly ThreadLocal<List<string>> _segmentBuffers = new(() => new(10));
    private readonly ThreadLocal<Dictionary<string, string>> _argumentsCollectors = new(() => new());

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

        var ogRequest = context.ListenerRequest;
        var httpMethod = GetHttpMethodFromHttpMethod(ogRequest);
        var segments = GetSegmentsFromAbsolutePath(ogRequest.Url!.AbsolutePath);
        var arguments = _argumentsCollectors.Value!;

        arguments.Clear();

        if (!MainBlueprint.TryFindRoute(httpMethod, CollectionsMarshal.AsSpan(segments), arguments, out var route))
            return false;

        var httpRequest = new Request.ApiHttpRequest(context);
        var httpContext = new RouteContext.ApiHttpRouteContext(httpRequest, httpMethod);

        route!.TryInvoke(httpContext, arguments, out var error, out var response);

        if (response != null)
            context.ServerResponse = response.AsHttpResponse?.HttpServerExResponse ?? null;
        else if (!string.IsNullOrEmpty(error))
        {
            Logging.Log(ELogSeverity.Error, $"Failed to execute route '{route.HttpEndpoint}': {error}");
            context.ServerResponse = Response.ApiHttpResponse.CodeResult(httpContext, HttpStatusCode.InternalServerError).HttpServerExResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a ApiHttpMethod from HttpMethod
    /// </summary>
    /// <param name="originalRequest">Original request</param>
    /// <returns>Equivalent ApiHttpMethod</returns>
    /// <exception cref="Exception">If no corresponding ApiHttpMethod was found</exception>
    private static Route.EApiHttpMethod GetHttpMethodFromHttpMethod(HttpListenerRequest originalRequest)
        => originalRequest.HttpMethod switch
        {
            "GET" => Route.EApiHttpMethod.Get,
            "DELETE" => Route.EApiHttpMethod.Delete,
            "POST" => Route.EApiHttpMethod.Post,
            "PUT" => Route.EApiHttpMethod.Put,
            "PATCH" => Route.EApiHttpMethod.Patch,
            _ => throw new Exception($"Unhandled HTTP method {originalRequest.HttpMethod}")
        };
    /// <summary>
    /// Get splitted segment from an absolute Url
    /// </summary>
    /// <param name="absolutePath">Absolut Url</param>
    /// <returns>List of segments</returns>
    protected List<string> GetSegmentsFromAbsolutePath(string absolutePath)
    {
        var result = _segmentBuffers.Value!;
        result.Clear();

        for (var i = 0; i < absolutePath.Length; ++i)
        {
            if (absolutePath[i] == '/')
                continue;

            var nextSeparator = absolutePath.IndexOf('/', i);
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

