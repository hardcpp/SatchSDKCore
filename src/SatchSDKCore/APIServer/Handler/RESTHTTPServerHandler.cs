using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.APIServer.Handler;

/// <summary>
/// REST HttpServerEx Server handler
/// </summary>
public class RESTHTTPServerHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.RESTBlueprint MainBlueprint = new Blueprint.RESTBlueprint("Main");

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
        var restMethod = GetRestMethodFromHttpMethod(ogRequest);
        var segments = GetSegmentsFromAbsolutePath(ogRequest.Url!.AbsolutePath);
        var arguments = _argumentsCollectors.Value!;

        arguments.Clear();

        if (!MainBlueprint.TryFindRoute(restMethod, CollectionsMarshal.AsSpan(segments), arguments, out var route))
            return false;

        var httpRequest = new Request.HTTPRequest(context);
        var restContext = new RouteContext.RESTRouteContext(httpRequest, restMethod);

        route!.TryInvoke(restContext, arguments, out var error, out var response);

        if (response != null)
            context.ServerResponse = response.AsRESTResponse?.HTTPServerResponse ?? null;
        else if (!string.IsNullOrEmpty(error))
        {
            Logging.Log(ELogSeverity.Error, $"Failed to execute route '{route.RESTEndpoint}': {error}");
            context.ServerResponse = Response.RESTResponse.CodeResult(restContext, HttpStatusCode.InternalServerError).HTTPServerResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a ERestMethod from HttpMethod
    /// </summary>
    /// <param name="originalRequest">Original request</param>
    /// <returns>Equivalent ERestMethod</returns>
    /// <exception cref="Exception">If no corresponding ERestMethod was found</exception>
    private static Route.ERestMethod GetRestMethodFromHttpMethod(HttpListenerRequest originalRequest)
    {
        switch (originalRequest.HttpMethod)
        {
            case "GET":
                return Route.ERestMethod.Get;

            case "DELETE":
                return Route.ERestMethod.Delete;

            case "POST":
                return Route.ERestMethod.Post;

            case "PUT":
                return Route.ERestMethod.Put;

            case "PATCH":
                return Route.ERestMethod.Patch;

            default:
                throw new Exception($"Unhandled HTTP method {originalRequest.HttpMethod}");
        }
    }
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
